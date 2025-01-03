using UnityEngine;

public class HandRaycast : MonoBehaviour
{
    [SerializeField]
    private OVRHand hand; // Referencia al componente OVRHand.

    [SerializeField]
    private float rayLength = 10f; // Longitud del raycast.

    [SerializeField]
    private LayerMask interactableLayer; // Capas con las que el raycast interactuará.

    // Variable pública para almacenar el último RaycastHit
    public RaycastHit LastHit { get; private set; }

    private LineRenderer lineRenderer; // Línea para visualizar el rayo.

    private void Start()
    {
        // Crear un LineRenderer para visualizar el rayo.
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        //lineRenderer.startWidth = 0.01f;
        //lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 2;

    }

    private void Update()
    {
        // Asegurarse de que la mano está siendo rastreada y que la pose del puntero es válida.
        if (hand == null || !hand.IsTracked || !hand.IsPointerPoseValid)
        {
            lineRenderer.enabled = false;
            return;
        }

        // Obtener la posición y orientación del rayo desde el OVRHand.
        Transform pointerPose = hand.PointerPose;
        Vector3 rayOrigin = pointerPose.position;
        Vector3 rayDirection = pointerPose.forward;

        // Mostrar el rayo en el mundo.
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, rayOrigin);
        lineRenderer.SetPosition(1, rayOrigin + rayDirection * rayLength);

        // Realizar el raycast.
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, rayLength, interactableLayer))
        {
            //Debug.Log($"Hit: {hit.collider.name}");
            LastHit = hit; // Guardamos el resultado del Raycast

        }
    }

  
}