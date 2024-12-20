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

    public void OnRoomMeshReady(MeshFilter mf)
    {
        _roomMeshfilter = mf;
        _mesh = mf.sharedMesh;

        if (PhotonNetwork.InRoom && canSend)
            EnviarMeshCollider();
    }

    public override void OnJoinedRoom()
    {
        _mesh = _roomMeshfilter.sharedMesh;
        if (canSend)
            EnviarMeshCollider();
    }

    private void EnviarMeshCollider()
    {
        List<Vector3> allVertices = new List<Vector3>(_mesh.vertices);
        List<int> allTriangles = new List<int>(_mesh.triangles);

        int fragmentSize = 500; // Tamaño de fragmento
        int totalFragments = Mathf.CeilToInt((float)allVertices.Count / fragmentSize);

        StartCoroutine(EnviarFragmentos(allVertices, allTriangles, fragmentSize, totalFragments));
    }

    private IEnumerator EnviarFragmentos(List<Vector3> vertices, List<int> triangles, int fragmentSize, int totalFragments)
    {
        canSend = false;

        for (int i = 0; i < totalFragments; i++)
        {
            // Dividir vértices
            Vector3[] vertexFragment = vertices
                .GetRange(i * fragmentSize, Mathf.Min(fragmentSize, vertices.Count - i * fragmentSize))
                .ToArray();

            // Ajustar triángulos al fragmento actual
            List<int> triangleFragment = new List<int>();
            int triangleStart = i * fragmentSize;
            int triangleEnd = triangleStart + vertexFragment.Length;

            foreach (int t in triangles)
            {
                if (t >= triangleStart && t < triangleEnd)
                {
                    triangleFragment.Add(t - triangleStart); // Ajustar índices al fragmento
                }
            }

            photonView.RPC(nameof(RecibirMeshColliderFragment_RPC), RpcTarget.Others, vertexFragment, triangleFragment.ToArray(), i, totalFragments);

            yield return new WaitForSeconds(0.1f); // Pausa para evitar saturar la red
        }

        photonView.RPC(nameof(EnviarMeshFinalizado_RPC), RpcTarget.Others);
    }

    [PunRPC]
    public void RecibirMeshColliderFragment_RPC(Vector3[] verticesFragment, int[] trianglesFragment, int fragmentIndex, int totalFragments)
    {
        Debug.Log($"Fragmento enviado: {fragmentIndex + 1}/{totalFragments}");
    }

    [PunRPC]
    public void EnviarMeshFinalizado_RPC()
    {
        Debug.Log("Envío de Mesh finalizado correctamente.");
    }

    [PunRPC]
    public void AcuseReciboMeshCollider_RPC()
    {
        canSend = true;
    }
}
