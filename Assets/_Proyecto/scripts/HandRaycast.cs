using UnityEngine;

public class HandRaycast : MonoBehaviour
{
    [SerializeField]
    private OVRHand hand; // Referencia al componente OVRHand.

    [SerializeField]
    private float rayLength = 10f; // Longitud del raycast.

    [SerializeField]
    private LayerMask interactableLayer; // Capas con las que el raycast interactuar�.

    [SerializeField]
    private Color rayColor = Color.red; // Color del rayo para depuraci�n.

    [SerializeField]
    private GameObject objectToInstantiate; // Prefab del objeto que se instanciar�.

    private LineRenderer lineRenderer; // L�nea para visualizar el rayo.

    private void Start()
    {
        // Crear un LineRenderer para visualizar el rayo.
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 2;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
    }

    private void Update()
    {
        // Asegurarse de que la mano est� siendo rastreada y que la pose del puntero es v�lida.
        if (hand == null || !hand.IsTracked || !hand.IsPointerPoseValid)
        {
            lineRenderer.enabled = false;
            return;
        }

        // Obtener la posici�n y orientaci�n del rayo desde el OVRHand.
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

            // Detectar el pinch para instanciar un objeto en la posici�n de colisi�n.
            if (hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            {
                InstantiateObjectAtHitPoint(hit.point, hit.normal);
            }
        }
    }

    private void InstantiateObjectAtHitPoint(Vector3 position, Vector3 normal)
    {
        if (objectToInstantiate != null)
        {
            // Instanciar el objeto en la posici�n del impacto con la orientaci�n alineada a la superficie.
            Quaternion rotation = Quaternion.LookRotation(normal); // Orientaci�n basada en la normal de la superficie.
            Instantiate(objectToInstantiate, position, rotation);
            //Debug.Log($"Object instantiated at: {position}");
        }
        else
        {
            Debug.LogWarning("No object assigned to instantiate.");
        }
    }
}