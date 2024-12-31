//using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BasicUtilities
{
    /// <summary>
    /// Crea un nuevo trazo de pintura con el mesh y color especificado.
    /// </summary>
    /// <param name="mesh">Mesh del trazo</param>
    /// <param name="colorIndex">Índice del color</param>
    /// <returns>GameObject del nuevo trazo</returns>
    public static GameObject CreateNewPaintMesh(Mesh mesh, int colorIndex, string paintMeshName, List<GameObject> paintMeshes, List<Material> paintMaterials, Stack<GameObject> undoStack, ref int currentSortingOrder)
    {
        GameObject newMesh = new GameObject(paintMeshName);

        newMesh.AddComponent<MeshFilter>().mesh = mesh;
        var renderer = newMesh.AddComponent<MeshRenderer>();
        renderer.material = paintMaterials[colorIndex];

        // Incrementar el orden de clasificación y establecerlo en el renderizador
        currentSortingOrder++;
        renderer.sortingOrder = currentSortingOrder;

        newMesh.layer = 11;

        // Añadir a la lista de trazos y a la pila de deshacer
        paintMeshes.Add(newMesh);
        undoStack.Push(newMesh);

        return newMesh;
    }

    public static Vector3 GetMouseWorldPosition(Camera camara, float drawingDepth = 1)
    {
        Ray ray = camara.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Dibuja el rayo en la escena para visualizarlo
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f); // Rayo visible en la escena

        // Si el raycast choca con algo, usar esa posición
        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log($"Colisionó con: {hit.collider.gameObject.name}"); // Muestra el nombre del objeto
            return hit.point; // Devuelve la posición de colisión
        }

        // Si no colisiona con nada, usar la distancia fija (drawingDepth)
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = camara.nearClipPlane + drawingDepth; // Profundidad a la que se situará el objeto
        return camara.ScreenToWorldPoint(mousePos);
    }




}
