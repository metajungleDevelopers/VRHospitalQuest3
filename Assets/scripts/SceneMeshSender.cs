using Photon.Pun;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PhotonView))]
public class SceneMeshSender : MonoBehaviourPunCallbacks
{
    public MeshFilter _roomMeshfilter;
    Mesh _mesh;
    bool canSend = true;

    private const int MAX_VERTICES_PER_CHUNK = 400;
    private const int MAX_TRIANGLES_PER_CHUNK = 800;
    private const float RETRY_DELAY = 2f; // Tiempo de espera antes de reenviar
    private const int MAX_RETRIES = 3;    // Máximo número de intentos de reenvío

    // Almacenamiento de chunks para reenvío
    private Vector3[] vertices;
    private int[] triangles;
    private Dictionary<int, int> vertexChunkRetries = new Dictionary<int, int>();
    private Dictionary<int, int> triangleChunkRetries = new Dictionary<int, int>();

    public void OnRoomMeshReady(MeshFilter mf)
    {
        _roomMeshfilter = mf;
        _mesh = mf.sharedMesh;
        if (PhotonNetwork.InRoom && canSend)
        {
            StopAllCoroutines();
            StartCoroutine(EnviarMeshColliderEnPartes());
        }     
    }

    private IEnumerator EnviarMeshColliderEnPartes()
    {
        if (_mesh == null) yield break;

        canSend = false;
        vertices = _mesh.vertices;
        triangles = _mesh.triangles;

        int numVertexChunks = Mathf.CeilToInt((float)vertices.Length / MAX_VERTICES_PER_CHUNK);
        int numTriangleChunks = Mathf.CeilToInt((float)triangles.Length / MAX_TRIANGLES_PER_CHUNK);

        // Inicializar contadores de reintentos
        vertexChunkRetries.Clear();
        triangleChunkRetries.Clear();

        // Enviar información inicial
        photonView.RPC(nameof(IniciarRecepcionMesh_RPC), RpcTarget.OthersBuffered,
            vertices.Length, triangles.Length, numVertexChunks, numTriangleChunks);

        yield return StartCoroutine(EnviarChunks());
    }

    private IEnumerator EnviarChunks()
    {
        int numVertexChunks = Mathf.CeilToInt((float)vertices.Length / MAX_VERTICES_PER_CHUNK);
        int numTriangleChunks = Mathf.CeilToInt((float)triangles.Length / MAX_TRIANGLES_PER_CHUNK);

        // Enviar vértices
        for (int i = 0; i < numVertexChunks; i++)
        {
            EnviarChunkVertices(i);
            Debug.Log("Paquete de vertices enviado");
            yield return new WaitForSeconds(0.05f);
        }

        // Enviar triángulos
        for (int i = 0; i < numTriangleChunks; i++)
        {
            EnviarChunkTriangulos(i);
            Debug.Log("Paquete de triangulos enviado");
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void EnviarChunkVertices(int chunkIndex)
    {
        int startIdx = chunkIndex * MAX_VERTICES_PER_CHUNK;
        int length = Mathf.Min(MAX_VERTICES_PER_CHUNK, vertices.Length - startIdx);
        Vector3[] vertexChunk = new Vector3[length];
        System.Array.Copy(vertices, startIdx, vertexChunk, 0, length);

        photonView.RPC(nameof(RecibirVertices_RPC), RpcTarget.Others,
            vertexChunk, chunkIndex);
    }

    private void EnviarChunkTriangulos(int chunkIndex)
    {
        int startIdx = chunkIndex * MAX_TRIANGLES_PER_CHUNK;
        int length = Mathf.Min(MAX_TRIANGLES_PER_CHUNK, triangles.Length - startIdx);
        int[] triangleChunk = new int[length];
        System.Array.Copy(triangles, startIdx, triangleChunk, 0, length);

        photonView.RPC(nameof(RecibirTriangulos_RPC), RpcTarget.Others,
            triangleChunk, chunkIndex);
    }

    [PunRPC]
    public void SolicitarReenvioVertices_RPC(int chunkIndex, int playerId)
    {
        // Solo procesar si somos el destinatario
        if (PhotonNetwork.LocalPlayer.ActorNumber != playerId) return;

        if (!vertexChunkRetries.ContainsKey(chunkIndex))
            vertexChunkRetries[chunkIndex] = 0;

        vertexChunkRetries[chunkIndex]++;

        if (vertexChunkRetries[chunkIndex] <= MAX_RETRIES)
        {
            StartCoroutine(ReenviarChunkVertices(chunkIndex));
        }
    }

    [PunRPC]
    public void SolicitarReenvioTriangulos_RPC(int chunkIndex, int playerId)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != playerId) return;

        if (!triangleChunkRetries.ContainsKey(chunkIndex))
            triangleChunkRetries[chunkIndex] = 0;

        triangleChunkRetries[chunkIndex]++;

        if (triangleChunkRetries[chunkIndex] <= MAX_RETRIES)
        {
            StartCoroutine(ReenviarChunkTriangulos(chunkIndex));
        }
    }

    private IEnumerator ReenviarChunkVertices(int chunkIndex)
    {
        yield return new WaitForSeconds(RETRY_DELAY);
        EnviarChunkVertices(chunkIndex);
    }

    private IEnumerator ReenviarChunkTriangulos(int chunkIndex)
    {
        yield return new WaitForSeconds(RETRY_DELAY);
        EnviarChunkTriangulos(chunkIndex);
    }

    [PunRPC]
    public void AcuseReciboMeshCollider_RPC()
    {
        canSend = true;
        // Limpiar datos almacenados
        vertices = null;
        triangles = null;
        vertexChunkRetries.Clear();
        triangleChunkRetries.Clear();
    }

    [PunRPC]
    public void EnvioDeMeshSolicitadoDesdeNuevoCliente()
    {
        if (canSend && _mesh != null)
        {
            StopAllCoroutines();
            StartCoroutine(EnviarMeshColliderEnPartes());
        }
    }

    [PunRPC]
    public void IniciarRecepcionMesh_RPC(int totalVertices, int totalTriangles,int numVertexChunks, int numTriangleChunks) { }

    [PunRPC]
    public void RecibirVertices_RPC(Vector3[] vertexChunk, int chunkIndex) { }

    [PunRPC]
    public void RecibirTriangulos_RPC(int[] triangleChunk, int chunkIndex) { }
}