using Photon.Pun;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PhotonView))]
public class ColliderSender : MonoBehaviourPunCallbacks //IHololensMeshColliderHandler
{
    public Transform cameraParent;
    public Transform OpenXRSpatialMeshObserver;
    public List<MeshFilter> meshFilters;
    public List<MeshCollider> meshColliders;
    public List<MeshCollider> lastSendmeshColliders;

    bool canSend = true;

    private void Start()
    {
        meshFilters = new List<MeshFilter>();
        meshColliders = new List<MeshCollider>();
        lastSendmeshColliders = new List<MeshCollider>();

        StartCoroutine(ColectarMeshes());
    }

    private IEnumerator ColectarMeshes()
    {
        while (true)
        {
            if(OpenXRSpatialMeshObserver != null)
            {
                meshFilters.Clear();
                meshColliders.Clear();
                // Recogemos todos los MeshFilter y MeshCollider del objeto especificado y sus hijos
                CollectMeshComponents(OpenXRSpatialMeshObserver, meshFilters, meshColliders);
            }
            else
            {
                //OpenXRSpatialMeshObserver = FindChildRecursive(cameraParent, "OpenXR Spatial Mesh Observer");
                Debug.Log("No hace nada");
            }

            yield return new WaitForSeconds(1.0f);
            EnviarMeshCollider();
        }
    }

    private void Update()
    {
        // Usar un m�todo que compare los contenidos de las listas en lugar de !=
        if (meshColliders != null && meshColliders.Count > 0 && PhotonNetwork.InRoom /*&& canSend*/ && !CompareMeshColliderLists(meshColliders, lastSendmeshColliders))
        {
            Debug.Log("SE ENVIA");
            
        }
    }

    private void EnviarMeshCollider()
    {
        try 
        {
            List<Vector3> allVertices = new List<Vector3>();
            List<int> allTriangles = new List<int>();
            int vertexOffset = 0;
            Debug.Log("EnviarMeshCollider");

            // Guardamos los MeshCollider enviados
            lastSendmeshColliders.Clear();
            lastSendmeshColliders.AddRange(meshColliders);

            // Combinamos los v�rtices y tri�ngulos de todos los MeshCollider
            foreach (MeshFilter meshfilter in meshFilters)
            {
                if (meshfilter != null && meshfilter.sharedMesh != null)
                {
                    Mesh mesh = meshfilter.sharedMesh;

                    // Agregamos los v�rtices al array total
                    allVertices.AddRange(mesh.vertices);

                    // Agregamos los tri�ngulos al array total, ajustando los �ndices
                    foreach (int triangle in mesh.triangles)
                    {
                        allTriangles.Add(triangle + vertexOffset);
                    }

                    // Actualizamos el offset para los pr�ximos tri�ngulos
                    vertexOffset += mesh.vertexCount;
                }
            }

            // Convertimos las listas a arrays para enviarlos a trav�s de Photon
            Vector3[] verticesArray = allVertices.ToArray();
            int[] trianglesArray = allTriangles.ToArray();

            canSend = false;
            // Enviar el mesh, posici�n y rotaci�n
            photonView.RPC(nameof(RecibirMeshCollider_RPC), RpcTarget.OthersBuffered, verticesArray, trianglesArray);
        }
        catch 
        {
            Debug.LogError("3333333");
        }
       
    }

    [PunRPC]
    public void RecibirMeshCollider_RPC(Vector3[] vertices, int[] triangles)
    {
        // Este m�todo se ejecutar� en el cliente que reconstruye el mesh
        // L�gica de reconstrucci�n del mesh (no necesaria en este c�digo)
    }

    [PunRPC]
    public void AcuseReciboMeshCollider_RPC()
    {
        canSend = true;
    }

    // M�todo p�blico para buscar un Transform por nombre
    public Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    // M�todo para encontrar y guardar todos los MeshFilter y MeshCollider en listas
    public void CollectMeshComponents(Transform root, List<MeshFilter> meshFilters, List<MeshCollider> meshColliders)
    {
        try 
        {
            MeshFilter meshFilter = root.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilters.Add(meshFilter);
            }

            MeshCollider meshCollider = root.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshColliders.Add(meshCollider);
            }

            foreach (Transform child in root)
            {
                CollectMeshComponents(child, meshFilters, meshColliders);
            }
        }
        catch 
        {
            Debug.LogError("Algo Fallo al buscar El MEsh");
        }
        
    }

    // M�todo para comparar el contenido de dos listas de MeshCollider
    private bool CompareMeshColliderLists(List<MeshCollider> current, List<MeshCollider> last)
    {
        try 
        {
            // Verificamos si las listas tienen diferentes tama�os
            if (current.Count != last.Count)
            {
                return false;
            }

            // Verificamos si los elementos son los mismos en el mismo orden
            for (int i = 0; i < current.Count; i++)
            {
                if (current[i] != last[i])
                {
                    return false;
                }
            }

            return true; // Si todos los elementos son iguales
        }
        catch {
            Debug.LogError("Algo fallo al comparar las listas de meshes"); return false;
            
        }
       
    }
}
