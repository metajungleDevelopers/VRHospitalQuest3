using MetaJungle.Utilities;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndoRedoHandler : SingletonMonoBehaviourPunCallbacks<UndoRedoHandler>
{
    public Stack<GameObject> undoStack = new Stack<GameObject>(); // Pila de deshacer
    public Stack<GameObject> redoStack = new Stack<GameObject>(); // Pila de deshacer

    public void UndoLastAction()
    {
        photonView.RPC(nameof(UndoLastAction_RPC), RpcTarget.AllBuffered);
        Debug.Log("UNDO STACK: "+ undoStack.Count);
    }

    public void RedoLastAction()
    {
        photonView.RPC(nameof(RedoLastAction_RPC), RpcTarget.AllBuffered);
    }

    [PunRPC]
    void UndoLastAction_RPC()
    {
        if (undoStack.Count > 0)
        {
            GameObject lastMesh = undoStack.Pop();

            if (lastMesh.activeSelf)
            {
                redoStack.Push(lastMesh);
                if (DrawingHandler.Instance != null) DrawingHandler.Instance.paintMeshes.Remove(lastMesh);
                if (HololensDrawingHandler.Instance != null) HololensDrawingHandler.Instance.paintMeshes.Remove(lastMesh);
                //ShapesHandler.Instance.shapeMeshes.Remove(lastMesh);
                lastMesh.SetActive(false);
            }
            else
            {
                UndoLastAction_RPC();
            }
            //hay que hacer que cuando lastmesh haya sido apagado por el eraser, salte al siguiente de manera recursiva hasta encontrar uno diferente a null
        }
    }

    [PunRPC]
    private void RedoLastAction_RPC()
    {
        if (redoStack.Count > 0)
        {
            GameObject lastMesh = redoStack.Pop();

            if (!lastMesh.activeSelf)
            {
                undoStack.Push(lastMesh);

                if(DrawingHandler.Instance != null)
                {
                    if (lastMesh.name == DrawingHandler.paintMeshName)
                    {
                        DrawingHandler.Instance.paintMeshes.Add(lastMesh);
                    }
                    else
                    {
                        //ShapesHandler.Instance.shapeMeshes.Add(lastMesh);
                    }
                }

                if (HololensDrawingHandler.Instance != null)
                {
                    if (lastMesh.name == HololensDrawingHandler.paintMeshName)
                    {
                        HololensDrawingHandler.Instance.paintMeshes.Add(lastMesh);
                    }
                    else
                    {
                        //ShapesHandler.Instance.shapeMeshes.Add(lastMesh);
                    }
                }

                lastMesh.SetActive(true);
            }
        }
    }
}
