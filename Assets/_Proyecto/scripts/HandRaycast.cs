using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandRaycast : MonoBehaviour
{
    public OVRHand rightHand; // Mano derecha para tracking
    public Transform rightController; // Controlador derecho
    public LayerMask drawLayerMask; // Capas donde se puede dibujar

    public Color drawColor = Color.red; // Color del dibujo
    public float brushSize = 0.1f; // Tamaño del pincel

    private bool isDrawing = false;

    void Update()
    {
        // Determinar si usamos Hand Tracking o el controlador
        bool isHandTracked = rightHand.IsTracked;
        Vector3 rayOrigin;
        Vector3 rayDirection;

        if (isHandTracked)
        {
            rayOrigin = rightHand.PointerPose.position;
            rayDirection = rightHand.PointerPose.forward;
        }
        else
        {
            rayOrigin = rightController.position;
            rayDirection = rightController.forward;
        }

        // Realizar Raycast
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, Mathf.Infinity, drawLayerMask))
        {
            Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.green);

            // Detectar si se está haciendo pinch o presionando el trigger
            if (isHandTracked && rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            {
                isDrawing = true;
            }
            else if (!isHandTracked && OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.5f)
            {
                isDrawing = true;
            }
            else
            {
                isDrawing = false;
            }

            // Dibujar en el Mesh si se está en modo de dibujo
            if (isDrawing)
            {
                DrawOnMesh(hit);
            }
        }
        else
        {
            Debug.DrawRay(rayOrigin, rayDirection * 10f, Color.red);
        }
    }

    void DrawOnMesh(RaycastHit hit)
    {
        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return;

        Mesh mesh = meshCollider.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Color[] colors = mesh.colors;

        if (colors.Length == 0)
        {
            // Si no hay colores definidos en el Mesh, inicializarlos
            colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.white; // Color base
            }
        }

        // Transformar el punto de impacto a las coordenadas locales del Mesh
        Vector3 localHitPoint = meshCollider.transform.InverseTransformPoint(hit.point);

        // Modificar los vértices cercanos al punto de impacto
        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(vertices[i], localHitPoint);
            if (distance < brushSize)
            {
                // Cambiar el color del vértice
                colors[i] = Color.Lerp(colors[i], drawColor, 0.5f);
            }
        }

        // Actualizar los colores del Mesh
        mesh.colors = colors;
    }
}
