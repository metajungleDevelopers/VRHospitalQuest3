using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class HandRaycast : MonoBehaviour
{
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private OVRHand rightHand;
    [SerializeField] private bool showDebugRay = true;

    // Variable pública para almacenar el último RaycastHit
    public RaycastHit LastHit { get; private set; }

    void Update()
    {
        if (rightHand.IsTracked)
        {
            Vector3 handPosition = rightHand.transform.position;
            Quaternion handRotation = rightHand.transform.rotation;
            Vector3 rayDirection = handRotation * Vector3.forward;

            if (Physics.Raycast(handPosition, rayDirection, out RaycastHit hit, rayLength, interactableLayer))
            {
                LastHit = hit; // Guardamos el resultado del Raycast
                Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");
            }

            if (showDebugRay)
            {
                Debug.DrawRay(handPosition, rayDirection * rayLength, Color.green);
            }
        }
    }
}
