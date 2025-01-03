using MetaJungle.Utilities;
//using Microsoft.MixedReality.Toolkit.Input;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EraserHandler : SingletonMonoBehaviourPunCallbacks<EraserHandler>
{
    public bool canErase = false;

    public float eraseRadius = 0.1f;

    public UnityEvent OnErasingActive;

    public bool isPc;

    /*private void Update()
    {
        if (canErase)
        {
            if (Input.GetMouseButton(0) && isPc)
            {
                // Intentar borrar en la posici�n actual del mouse
                TryEraseAtPosition(DrawingHandler.Instance.worldPosition);
            }

            if(HololensDrawingHandler.Instance != null)
            {
                if (HololensPinchDetector.Instance.isClickHeld && !isPc)
                {
                    // Intentar borrar
                    TryEraseAtPosition_Hololens(HololensPinchDetector.Instance.currentPointer);
                }
            }
        }      
    }

    /// <summary>
    /// Intenta borrar los trazos en la posici�n dada.
    /// Env�a un RPC solo si hay trazos en el �rea de borrado.
    /// </summary>
    /// <param name="position">Posici�n en el mundo donde se intentar� borrar</param>
    private void TryEraseAtPosition(Vector3 position)
    {
        List<GameObject> meshesToRemove = new List<GameObject>();
        List<GameObject> shapesToRemove = new List<GameObject>();

        // Buscar trazos dentro del �rea de borrado
        GetGameObjectsToRemove(position, ref meshesToRemove, ref shapesToRemove);

        // Enviar RPC solo si hay algo que borrar
        if (meshesToRemove.Count > 0 || shapesToRemove.Count > 0)
        {
            photonView.RPC(nameof(EraseAtPosition_RPC), RpcTarget.AllBuffered, position);
        }
    }

    /// <summary>
    /// RPC para borrar trazos en la posici�n dada.
    /// </summary>
    /// <param name="position">Posici�n en el mundo donde se intentara borrar</param>
    [PunRPC]
    void EraseAtPosition_RPC(Vector3 position)
    {
        List<GameObject> meshesToRemove = new List<GameObject>();
        List<GameObject> shapesToRemove = new List<GameObject>();

        GetGameObjectsToRemove(position, ref meshesToRemove, ref shapesToRemove);

        // Eliminar los trazos encontrados
        foreach (var meshObject in meshesToRemove)
        {
            if (DrawingHandler.Instance != null) DrawingHandler.Instance.paintMeshes.Remove(meshObject);
            if (HololensDrawingHandler.Instance != null) HololensDrawingHandler.Instance.paintMeshes.Remove(meshObject);
            UndoRedoHandler.Instance.redoStack.Push(meshObject);
            meshObject.SetActive(false);
        }

        // Eliminar los shapes encontrados
        foreach (var shapeObject in shapesToRemove)
        {
            ShapesHandler.Instance.shapeMeshes.Remove(shapeObject);
            UndoRedoHandler.Instance.redoStack.Push(shapeObject);
            shapeObject.SetActive(false);
        }
    }

    private void GetGameObjectsToRemove(Vector3 position, ref List<GameObject> meshesToRemove, ref List<GameObject> shapesToRemove)
    {
        if (DrawingHandler.Instance != null)
        {
            // Buscar trazos dentro del �rea de borrado
            foreach (var meshObject in DrawingHandler.Instance.paintMeshes)
            {
                Mesh mesh = meshObject.GetComponent<MeshFilter>().mesh;
                Vector3[] vertices = mesh.vertices;

                foreach (var vertex in vertices)
                {
                    if (Vector3.Distance(position, meshObject.transform.TransformPoint(vertex)) <= eraseRadius)
                    {
                        meshesToRemove.Add(meshObject);
                        break;
                    }
                }
            }
        }

        if (HololensDrawingHandler.Instance != null)
        {
            // Buscar trazos dentro del �rea de borrado
            foreach (var meshObject in HololensDrawingHandler.Instance.paintMeshes)
            {
                Mesh mesh = meshObject.GetComponent<MeshFilter>().mesh;
                Vector3[] vertices = mesh.vertices;

                foreach (var vertex in vertices)
                {
                    if (Vector3.Distance(position, meshObject.transform.TransformPoint(vertex)) <= eraseRadius)
                    {
                        meshesToRemove.Add(meshObject);
                        break;
                    }
                }
            }
        }

        foreach (var shapeObject in ShapesHandler.Instance.shapeMeshes)
        {
            Mesh mesh = shapeObject.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;

            foreach (var vertex in vertices)
            {
                if (Vector3.Distance(position, shapeObject.transform.TransformPoint(vertex)) <= eraseRadius * 2)
                {
                    shapesToRemove.Add(shapeObject);
                    break;
                }
            }
        }
    }

    public void ActivateEraser()
    {
        canErase = true;  
        if (DrawingHandler.Instance != null) DrawingHandler.Instance.canDraw = false; // Desactiva el dibujo si el borrador est� activado
        if (HololensDrawingHandler.Instance != null) HololensDrawingHandler.Instance.canDraw = false;
        ShapesHandler.Instance.canCreateShape = false;

        OnErasingActive.Invoke();
    }

    /// <summary>
    /// Intenta borrar los trazos en la posici�n dada.
    /// Env�a un RPC solo si hay trazos en el �rea de borrado.
    /// </summary>
    /// <param name="position">Posici�n en el mundo donde se intentar� borrar</param>
    private void TryEraseAtPosition_Hololens(IMixedRealityPointer pointPosition)
    {
        List<GameObject> meshesToRemove = new List<GameObject>();
        List<GameObject> shapesToRemove = new List<GameObject>();

        if (pointPosition != null)
        {
            if (pointPosition.Result != null)
            {
                if (DrawingHandler.Instance != null) DrawingHandler.Instance.worldPosition = pointPosition.Result.Details.Point;
                if (HololensDrawingHandler.Instance != null) HololensDrawingHandler.Instance.worldPosition = pointPosition.Result.Details.Point;

                // Buscar trazos dentro del �rea de borrado
                if (DrawingHandler.Instance != null) GetGameObjectsToRemove(DrawingHandler.Instance.worldPosition, ref meshesToRemove, ref shapesToRemove);
                if (HololensDrawingHandler.Instance != null) GetGameObjectsToRemove(HololensDrawingHandler.Instance.worldPosition, ref meshesToRemove, ref shapesToRemove);

                // Enviar RPC solo si hay algo que borrar
                if (meshesToRemove.Count > 0 || shapesToRemove.Count > 0)
                {
                    if (DrawingHandler.Instance != null)
                        photonView.RPC(nameof(EraseAtPosition_RPC), RpcTarget.AllBuffered, DrawingHandler.Instance.worldPosition);
                    if (HololensDrawingHandler.Instance != null)
                        photonView.RPC(nameof(EraseAtPosition_RPC), RpcTarget.AllBuffered, HololensDrawingHandler.Instance.worldPosition);
                }
            }
            else
            {
                HololensPinchDetector.Instance.isClickHeld = false;
            }
        }
        else
        {
            HololensPinchDetector.Instance.isClickHeld = false;
        }
    }*/
}
