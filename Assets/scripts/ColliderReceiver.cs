using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class ColliderReceiver : MonoBehaviourPunCallbacks
{
    public Material wireFrimeMaterial;
    public bool showWireframe;

    private List<Vector3> receivedVertices = new List<Vector3>();
    private List<int> receivedTriangles = new List<int>();
    private int fragmentsReceived = 0;
    private int totalExpectedFragments = 0;

    [PunRPC]
    public void RecibirMeshColliderFragment_RPC(Vector3[] verticesFragment, int[] trianglesFragment, int fragmentIndex, int totalFragments)
    {
        Debug.Log($"Fragmento recibido: {fragmentIndex + 1}/{totalFragments}");

        if (verticesFragment.Length == 0 || trianglesFragment.Length == 0)
        {
            Debug.LogError($"Fragmento {fragmentIndex + 1} contiene datos vacíos.");
            return;
        }

        int maxIndex = Mathf.Max(trianglesFragment);
        if (maxIndex >= receivedVertices.Count + verticesFragment.Length)
        {
            Debug.LogError($"Fragmento {fragmentIndex + 1} tiene índices de triángulos fuera de los límites.");
            return;
        }

        receivedVertices.AddRange(verticesFragment);
        receivedTriangles.AddRange(trianglesFragment);

        fragmentsReceived++;
        totalExpectedFragments = totalFragments;

        if (fragmentsReceived == totalExpectedFragments)
        {
            ReconstruirMesh();
        }
    }

    private void ReconstruirMesh()
    {
        GameObject newColliderObject = new GameObject("RemoteMeshCollider");
        Mesh mesh = new Mesh();

        // Verificar datos antes de ensamblar
        if (receivedTriangles.Exists(t => t >= receivedVertices.Count || t < 0))
        {
            Debug.LogError("Índice de triángulo fuera de rango de los vértices recibidos.");
            return;
        }

        // Validar índices de triángulos
        foreach (int index in receivedTriangles)
        {
            if (index < 0 || index >= receivedVertices.Count)
            {
                Debug.LogError($"Índice fuera de rango: {index}. Total vértices: {receivedVertices.Count}");
                return; // Detener la ejecución si hay índices inválidos
            }
        }

        mesh.vertices = receivedVertices.ToArray();
        mesh.triangles = receivedTriangles.ToArray();

        // Recalcular normales y límites
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Asignar el Mesh al MeshFilter y MeshCollider
        MeshFilter meshFilter = newColliderObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshCollider meshCollider = newColliderObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        // Opcional: Renderizar el wireframe
        if (showWireframe)
        {
            MeshRenderer renderer = newColliderObject.AddComponent<MeshRenderer>();
            renderer.material = wireFrimeMaterial;
        }

        // Limpieza
        receivedVertices.Clear();
        receivedTriangles.Clear();
        fragmentsReceived = 0;
        totalExpectedFragments = 0;

        Debug.Log($"Mesh ensamblado correctamente con {mesh.vertexCount} vértices y {mesh.triangles.Length / 3} triángulos.");
    }



    [PunRPC]
    public void EnviarMeshFinalizado_RPC()
    {
        Debug.Log("Mesh recibido y ensamblado correctamente.");
    }

    [PunRPC]
    public void AcuseReciboMeshCollider_RPC() { }
}
