using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PCCaptureDevice : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public RawImage rawImage;
    private WebCamTexture webCamTexture;

    void Start()
    {
        // Obtiene la lista de dispositivos de captura
        WebCamDevice[] devices = WebCamTexture.devices;

        // Limpia las opciones actuales del dropdown
        dropdown.ClearOptions();

        // Crea una lista para las opciones del dropdown
        List<string> options = new List<string>();

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
            StartCoroutine(StartWebCam(devices[0].name));
        }
    }

    void OnDropdownValueChanged(int index)
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        StartCoroutine(StartWebCam(devices[index].name));
    }

    private IEnumerator StartWebCam(string deviceName)
    {
        if (webCamTexture != null)
        {
            rawImage.texture = null;
            webCamTexture.Stop();
            webCamTexture = null;
        }

        webCamTexture = new WebCamTexture(deviceName, 1280, 720);
        webCamTexture.Play();

        // Espera hasta que la cámara esté lista
        while (!webCamTexture.isPlaying || webCamTexture.width <= 16)
        {
            yield return null;
        }

        rawImage.texture = webCamTexture;
        rawImage.material.mainTexture = webCamTexture;
    }

    private void OnDestroy()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }
}
