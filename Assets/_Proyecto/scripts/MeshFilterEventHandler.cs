using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;

public class MeshFilterEventHandler : MonoBehaviourPunCallbacks
{
    // Define un UnityEvent que devuelve un Object (se castea despu�s a MeshFilter)
    public UnityEvent<MeshFilter> OnMeshFilterRequested;

    // MeshFilter que se usar� en el evento
    [Header("El meshFilter que se pasara con el evento")]
    [SerializeField] private MeshFilter targetMeshFilter;

    private void Start()
    {
        // Verifica si el MeshFilter est� asignado
        if (targetMeshFilter == null)
        {
            targetMeshFilter = GetComponent<MeshFilter>();
        }

        if (targetMeshFilter == null)
        {
            Debug.LogError("No se encontr� un MeshFilter en este objeto ni fue asignado uno.");
        }
    }

    public override void OnJoinedRoom()
    {
        TriggerMeshFilterEvent();
    }

    [ContextMenu("TEST")]
    // M�todo para invocar el evento
    public void TriggerMeshFilterEvent()
    {
        if (targetMeshFilter != null)
        {
            Debug.Log("Disparando UnityEvent con MeshFilter como Object.");
            OnMeshFilterRequested?.Invoke(targetMeshFilter);
        }
        else
        {
            Debug.LogError("No se puede disparar el UnityEvent porque no hay un MeshFilter disponible.");
        }
    }


}
