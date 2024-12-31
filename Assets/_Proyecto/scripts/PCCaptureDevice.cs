using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PCCaptureDevice : MonoBehaviour
{
    public TMP_Dropdown dropdown; // Referencia al componente Dropdown
    public RawImage rawImage; // Referencia al componente RawImage para mostrar la webcam
    private WebCamTexture webCamTexture; // Variable para WebCamTexture

    void Start()
    {
        // Obtiene la lista de dispositivos de captura
        WebCamDevice[] devices = WebCamTexture.devices;

        // Limpia las opciones actuales del dropdown
        dropdown.ClearOptions();

        // Crea una lista para las opciones del dropdown
        List<string> options = new List<string>();

        // Añade el nombre de cada dispositivo a la lista de opciones
        foreach (WebCamDevice device in devices)
        {
            options.Add(device.name);
        }

        // Añade las opciones al dropdown
        dropdown.AddOptions(options);

        // Configura el evento para cuando se seleccione una opción
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        // Si hay al menos un dispositivo, inicia la primera cámara
        if (options.Count > 0)
        {
            webCamTexture = new WebCamTexture(devices[0].name);
            rawImage.texture = webCamTexture;
            webCamTexture.Play();
        }
    }

    // Método que se llama cuando se cambia la opción en el dropdown
    void OnDropdownValueChanged(int index)
    {
        // Detiene la cámara actual si está reproduciendo
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }

        // Crea una nueva instancia de WebCamTexture con la cámara seleccionada
        WebCamDevice[] devices = WebCamTexture.devices;
        webCamTexture = new WebCamTexture(devices[index].name);

        // Asigna la nueva WebCamTexture al RawImage y la inicia
        rawImage.texture = webCamTexture;
        webCamTexture.Play();
    }

    private void OnDestroy()
    {
        // Detiene la cámara cuando se destruye el objeto
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }
}
