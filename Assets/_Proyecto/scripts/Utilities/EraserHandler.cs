using MetaJungle.Utilities;
//using Microsoft.MixedReality.Toolkit.Input;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Events;

public class EraserHandler : SingletonMonoBehaviourPunCallbacks<EraserHandler>
{
    public bool canErase = false;

    public float eraseRadius = 0.1f;

    public UnityEvent OnErasingActive;

    [Header("Quest Input")]
    [SerializeField] private OVRHand rightHand;
    [SerializeField] private OVRInput.Controller rightController;
    [SerializeField] private float pinchThreshold = 0.7f;
    public HandRaycast handRaycast;



    public bool isPc;

    private void Update()
    {
        if (canErase)
        {
            if (Input.GetMouseButton(0) && isPc)
            {
                // Intentar borrar en la posición actual del mouse
                TryEraseAtPosition(DrawingHandler.Instance.worldPosition);
            }

            if(HololensDrawingHandler.Instance != null)
            {
                if (rightHand != null && rightHand.IsTracked)
                {
                    float pinchStrength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
                    if (pinchStrength > pinchThreshold)
                    {
                        // Intentar borrar
                        TryEraseAtPosition_Hololens();
                    }
                   
                }
            }
        }      
    }

    /// <summary>
    /// Intenta borrar los trazos en la posición dada.
    /// Envía un RPC solo si hay trazos en el área de borrado.
    /// </summary>
    /// <param name="position">Posición en el mundo donde se intentará borrar</param>
    private void TryEraseAtPosition(Vector3 position)
    {
        List<GameObject> meshesToRemove = new List<GameObject>();
        List<GameObject> shapesToRemove = new List<GameObject>();

        // Buscar trazos dentro del área de borrado
        GetGameObjectsToRemove(position, ref meshesToRemove);

        // Enviar RPC solo si hay algo que borrar
        if (meshesToRemove.Count > 0 || shapesToRemove.Count > 0)
        {
            photonView.RPC(nameof(EraseAtPosition_RPC), RpcTarget.AllBuffered, position);
        }
    }

    /// <summary>
    /// RPC para borrar trazos en la posición dada.
    /// </summary>
    /// <param name="position">Posición en el mundo donde se intentara borrar</param>
    [PunRPC]
    void EraseAtPosition_RPC(Vector3 position)
    {
        List<GameObject> meshesToRemove = new List<GameObject>();
        //List<GameObject> shapesToRemove = new List<GameObject>();

        GetGameObjectsToRemove(position, ref meshesToRemove);

        // Eliminar los trazos encontrados
        foreach (var meshObject in meshesToRemove)
        {
            if (DrawingHandler.Instance != null) DrawingHandler.Instance.paintMeshes.Remove(meshObject);
            if (HololensDrawingHandler.Instance != null) HololensDrawingHandler.Instance.paintMeshes.Remove(meshObject);
            UndoRedoHandler.Instance.redoStack.Push(meshObject);
            meshObject.SetActive(false);
        }

        // Eliminar los shapes encontrados
        /*foreach (var shapeObject in shapesToRemove)
        {
            //ShapesHandler.Instance.shapeMeshes.Remove(shapeObject);
            UndoRedoHandler.Instance.redoStack.Push(shapeObject);
            shapeObject.SetActive(false);
        }*/
    }

    private void GetGameObjectsToRemove(Vector3 position, ref List<GameObject> meshesToRemove)
    {
        if (DrawingHandler.Instance != null)
        {
            // Buscar trazos dentro del área de borrado
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
            // Buscar trazos dentro del área de borrado
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

        /*foreach (var shapeObject in ShapesHandler.Instance.shapeMeshes)
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
        }*/
    }

    public void ActivateEraser()
    {
        canErase = true;  
        //if (DrawingHandler.Instance != null) DrawingHandler.Instance.canDraw = false; // Desactiva el dibujo si el borrador está activado
        //if (HololensDrawingHandler.Instance != null) HololensDrawingHandler.Instance.canDraw = false;
        //ShapesHandler.Instance.canCreateShape = false;

        OnErasingActive.Invoke();
    }

    /// <summary>
    /// Intenta borrar los trazos en la posición dada.
    /// Envía un RPC solo si hay trazos en el área de borrado.
    /// </summary>
    /// <param name="position">Posición en el mundo donde se intentará borrar</param>
    private void TryEraseAtPosition_Hololens()
    {
        List<GameObject> meshesToRemove = new List<GameObject>();

        // Asegúrate de que el raycast haya detectado un impacto
        if (handRaycast.LastHit.collider != null)
        {
            // Obtener la posición del impacto
            Vector3 hitPosition = handRaycast.LastHit.point;

            // Actualizar las posiciones en los handlers
            if (DrawingHandler.Instance != null)
                DrawingHandler.Instance.worldPosition = hitPosition;
            if (HololensDrawingHandler.Instance != null)
                HololensDrawingHandler.Instance.worldPosition = hitPosition;

            // Buscar trazos dentro del área de borrado
            if (DrawingHandler.Instance != null)
                GetGameObjectsToRemove(DrawingHandler.Instance.worldPosition, ref meshesToRemove);
            if (HololensDrawingHandler.Instance != null)
                GetGameObjectsToRemove(HololensDrawingHandler.Instance.worldPosition, ref meshesToRemove);

            // Enviar RPC solo si hay algo que borrar
            if (meshesToRemove.Count > 0)
            {
                if (DrawingHandler.Instance != null)
                    photonView.RPC(nameof(EraseAtPosition_RPC), RpcTarget.AllBuffered, DrawingHandler.Instance.worldPosition);
                if (HololensDrawingHandler.Instance != null)
                    photonView.RPC(nameof(EraseAtPosition_RPC), RpcTarget.AllBuffered, HololensDrawingHandler.Instance.worldPosition);
            }
        }
    }

}
