using UnityEngine;
using TMPro;

public class VertexCounter : MonoBehaviour
{
    [Header("Object to Analyze")]
    public GameObject targetObject; // Objeto cuyo número de vértices se contará

    [Header("UI Element")]
    public TextMeshProUGUI vertexCountText; // Componente TextMeshPro para mostrar el conteo

    void Start()
    {
        if (targetObject == null || vertexCountText == null)
        {
            Debug.LogError("Por favor, asigna un objeto y un componente TextMeshProUGUI en el inspector.");
            return;
        }

        int vertexCount = CountVertices(targetObject);
        vertexCountText.text = "Vértices: " + vertexCount.ToString();
    }

    private void Update()
    {
        if(targetObject == null) 
        {
            targetObject = GameObject.Find("PREFAB MESH");
        }
        else 
        {
            int vertexCount = CountVertices(targetObject);
            vertexCountText.text = "Vértices: " + vertexCount.ToString();
        }
    }

    int CountVertices(GameObject obj)
    {
        int totalVertices = 0;

        // Recorremos todos los MeshFilters del objeto (incluidos los hijos)
        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh != null)
            {
                totalVertices += meshFilter.sharedMesh.vertexCount;
            }
        }

        return totalVertices;
    }
}
