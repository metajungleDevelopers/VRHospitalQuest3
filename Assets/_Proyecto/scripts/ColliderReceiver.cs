using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class ColliderReceiver : MonoBehaviourPunCallbacks
{
    public Material wireFrimeMaterial;
    public bool showWireframe;
    private List<GameObject> colliders = new List<GameObject>();

    private Vector3[] receivedVertices;
    private int[] receivedTriangles;
    private bool[] receivedVertexChunks;
    private bool[] receivedTriangleChunks;
    private int expectedVertexChunks;
    private int expectedTriangleChunks;
    private bool isReceivingMesh = false;

    private const int MAX_VERTICES_PER_CHUNK = 400;
    private const int MAX_TRIANGLES_PER_CHUNK = 800;
    private const float CHECK_MISSING_INTERVAL = 2f;
    private Coroutine checkMissingChunksCoroutine;
    private int senderPlayerId;

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        photonView.RPC(nameof(EnvioDeMeshSolicitadoDesdeNuevoCliente), RpcTarget.OthersBuffered);
    }

    [PunRPC]
    public void EnvioDeMeshSolicitadoDesdeNuevoCliente() { }


    [PunRPC]
    public void IniciarRecepcionMesh_RPC(int totalVertices, int totalTriangles,
        int numVertexChunks, int numTriangleChunks)
    {
        if (checkMissingChunksCoroutine != null)
            StopCoroutine(checkMissingChunksCoroutine);

        Debug.Log($"Iniciando recepci�n de mesh: {totalVertices} v�rtices, {totalTriangles} tri�ngulos");

        receivedVertices = new Vector3[totalVertices];
        receivedTriangles = new int[totalTriangles];
        receivedVertexChunks = new bool[numVertexChunks];
        receivedTriangleChunks = new bool[numTriangleChunks];
        expectedVertexChunks = numVertexChunks;
        expectedTriangleChunks = numTriangleChunks;
        isReceivingMesh = true;
        senderPlayerId = PhotonNetwork.MasterClient.ActorNumber;

        checkMissingChunksCoroutine = StartCoroutine(CheckMissingChunks());
    }

    private IEnumerator CheckMissingChunks()
    {
        int consecutiveCompleteCycles = 0;
        while (isReceivingMesh)
        {
            yield return new WaitForSeconds(CHECK_MISSING_INTERVAL);

            bool hayChunksFaltantes = false;

            // Verificar v�rtices faltantes
            for (int i = 0; i < receivedVertexChunks.Length; i++)
            {
                if (!receivedVertexChunks[i])
                {
                    hayChunksFaltantes = true;
                    Debug.Log($"Solicitando reenv�o de chunk de v�rtices {i}");
                    photonView.RPC(nameof(SolicitarReenvioVertices_RPC),
                        RpcTarget.All, i, senderPlayerId);
                }
            }

            // Verificar tri�ngulos faltantes
            for (int i = 0; i < receivedTriangleChunks.Length; i++)
            {
                if (!receivedTriangleChunks[i])
                {
                    hayChunksFaltantes = true;
                    Debug.Log($"Solicitando reenv�o de chunk de tri�ngulos {i}");
                    photonView.RPC(nameof(SolicitarReenvioTriangulos_RPC),
                        RpcTarget.All, i, senderPlayerId);
                }
            }

            // Si no hay chunks faltantes, incrementar el contador
            if (!hayChunksFaltantes)
            {
                consecutiveCompleteCycles++;
                if (consecutiveCompleteCycles >= 2) // Si todo est� completo por 2 ciclos, terminar
                {
                    Debug.Log("Todos los chunks han sido recibidos correctamente");
                    isReceivingMesh = false;
                    break;
                }
            }
            else
            {
                consecutiveCompleteCycles = 0;
            }
        }
    }

    [PunRPC]
    public void RecibirVertices_RPC(Vector3[] vertexChunk, int chunkIndex)
    {
        if (!isReceivingMesh) return;

        int startIdx = chunkIndex * MAX_VERTICES_PER_CHUNK;
        if (startIdx + vertexChunk.Length > receivedVertices.Length)
        {
            Debug.LogError($"�ndice de v�rtices fuera de rango: {startIdx + vertexChunk.Length} > {receivedVertices.Length}");
            return;
        }

        System.Array.Copy(vertexChunk, 0, receivedVertices, startIdx, vertexChunk.Length);
        receivedVertexChunks[chunkIndex] = true;

        Debug.Log($"Chunk de v�rtices {chunkIndex} recibido. Progreso: {ContarChunksRecibidos(receivedVertexChunks)}/{expectedVertexChunks}");

        CheckCompleteMesh();
    }

    [PunRPC]
    public void RecibirTriangulos_RPC(int[] triangleChunk, int chunkIndex)
    {
        if (!isReceivingMesh) return;

        int startIdx = chunkIndex * MAX_TRIANGLES_PER_CHUNK;
        if (startIdx + triangleChunk.Length > receivedTriangles.Length)
        {
            Debug.LogError($"�ndice de tri�ngulos fuera de rango: {startIdx + triangleChunk.Length} > {receivedTriangles.Length}");
            return;
        }

        System.Array.Copy(triangleChunk, 0, receivedTriangles, startIdx, triangleChunk.Length);
        receivedTriangleChunks[chunkIndex] = true;

        Debug.Log($"Chunk de tri�ngulos {chunkIndex} recibido. Progreso: {ContarChunksRecibidos(receivedTriangleChunks)}/{expectedTriangleChunks}");

        CheckCompleteMesh();
    }

    private int ContarChunksRecibidos(bool[] chunks)
    {
        int count = 0;
        foreach (bool chunk in chunks)
        {
            if (chunk) count++;
        }
        return count;
    }

    private void CheckCompleteMesh()
    {
        bool verticesComplete = System.Array.TrueForAll(receivedVertexChunks, x => x);
        bool trianglesComplete = System.Array.TrueForAll(receivedTriangleChunks, x => x);

        if (verticesComplete && trianglesComplete)
        {
            if (checkMissingChunksCoroutine != null)
                StopCoroutine(checkMissingChunksCoroutine);

            ValidateAndCreateMeshCollider();
        }
    }

    private void ValidateAndCreateMeshCollider()
    {
        // Validar que todos los �ndices de tri�ngulos sean v�lidos
        bool indicesValidos = true;
        for (int i = 0; i < receivedTriangles.Length; i++)
        {
            if (receivedTriangles[i] < 0 || receivedTriangles[i] >= receivedVertices.Length)
            {
                Debug.LogError($"�ndice de tri�ngulo inv�lido en la posici�n {i}: {receivedTriangles[i]}");
                indicesValidos = false;
                break;
            }
        }

        if (!indicesValidos)
        {
            Debug.LogError("Mesh inv�lido: �ndices de tri�ngulos fuera de rango");
            return;
        }

        CrearMeshCollider();
    }

    private void CrearMeshCollider()
    {
        foreach (GameObject go in colliders)
        {
            Destroy(go);
        }
        colliders.Clear();

        GameObject newColliderObject = new GameObject("RemoteMeshCollider");
        newColliderObject.transform.position = Vector3.zero;
        newColliderObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // Establecer rotaci�n x = -90, y = 0, z = 0
        newColliderObject.transform.localScale = Vector3.one;

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Permite m�s de 65535 v�rtices
        mesh.vertices = receivedVertices;
        mesh.triangles = receivedTriangles;

        // Recalcular normales y bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter meshFilter = newColliderObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshCollider meshCollider = newColliderObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;

        if (showWireframe)
        {
            MeshRenderer meshRenderer = newColliderObject.AddComponent<MeshRenderer>();
            meshRenderer.material = wireFrimeMaterial;
        }

        Debug.Log($"Mesh creado exitosamente: {mesh.vertices.Length} v�rtices, {mesh.triangles.Length / 3} tri�ngulos");

        colliders.Add(newColliderObject);
        photonView.RPC(nameof(AcuseReciboMeshCollider_RPC), RpcTarget.OthersBuffered);

        // Limpiar variables
        isReceivingMesh = false;
        receivedVertices = null;
        receivedTriangles = null;
        receivedVertexChunks = null;
        receivedTriangleChunks = null;
    }


    [PunRPC]
    public void AcuseReciboMeshCollider_RPC() { }

    [PunRPC]
    public void SolicitarReenvioVertices_RPC(int chunkIndex, int playerId) { }

    [PunRPC]
    public void SolicitarReenvioTriangulos_RPC(int chunkIndex, int playerId) { }
}