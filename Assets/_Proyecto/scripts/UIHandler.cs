using System;
using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    [Serializable]
    private class BotonUI
    {
        public MeshRenderer imagenBoton;
        public Texture spriteSinPresionar;
        public Texture spritePresionado;
    }

    [Space]
    [SerializeField] private BotonUI pincel;
    [SerializeField] private GameObject menuPincel;
    [HideInInspector] public bool pincelActivado;

    [Space]
    [SerializeField] private BotonUI borrar;
    private bool borrarActivado;

    [Space]
    [SerializeField] private BotonUI formas;
    [SerializeField] private GameObject menuFormas;
    private bool formasActivado;

    [Space]
    [Header("Forma Circulo para boton directo de hololens")]
    [SerializeField] private BotonUI circulo;
    private bool circuloActivado;

    [Space]
    [Header("Forma Flecha para boton directo de hololens")]
    [SerializeField] private BotonUI flecha;
    private bool flechaActivado;

    [Space]
    [Header("Forma puntero para boton directo de hololens")]
    [SerializeField] private BotonUI pointer;
    private bool pointerActivado;

    [Space]
    [SerializeField] private BotonUI fichaMedica;
    [SerializeField] private GameObject ficha;
    [SerializeField] private GameObject escaner;
    [SerializeField] private GameObject ingresoRut;
    private bool fichaActivado;

    [Space]
    [SerializeField] private BotonUI streaming;
    private bool streamingActivado;

    // Método para resetear todos los estados
    private void ResetAll()
    {
        if(menuFormas != null) menuFormas.SetActive(false);
        if(menuPincel != null) menuPincel.SetActive(false);
        pincelActivado = false;
        formasActivado = false;
        borrarActivado = false;
        //fichaActivado = false;
        circuloActivado = false;
        flechaActivado = false;
        pointerActivado = false;
        //streamingActivado = false;

        pincel.imagenBoton.material.mainTexture = pincel.spriteSinPresionar;
        borrar.imagenBoton.material.mainTexture = borrar.spriteSinPresionar;
        if (formas.imagenBoton != null) formas.imagenBoton.material.mainTexture = formas.spriteSinPresionar;
        if (circulo.imagenBoton != null) circulo.imagenBoton.material.mainTexture = circulo.spriteSinPresionar;
        if (flecha.imagenBoton != null) flecha.imagenBoton.material.mainTexture = flecha.spriteSinPresionar;
        if (pointer.imagenBoton != null) pointer.imagenBoton.material.mainTexture = pointer.spriteSinPresionar;
        //if (streaming.imagenBoton != null) streaming.imagenBoton.material.mainTexture = streaming.spriteSinPresionar;
        //fichaMedica.imagenBoton.material.mainTexture = fichaMedica.spriteSinPresionar;
    }

    // Método para actualizar el estado y el sprite del botón
    private void ActivarDesactivar(BotonUI boton, GameObject menu, ref bool estado)
    {
        // Si el botón ya está activado, simplemente lo desactiva y devuelve
        if (estado)
        {
            estado = false;
            if (menu != null)
            {
                menu.SetActive(false);
            }
            boton.imagenBoton.material.mainTexture = boton.spriteSinPresionar;
            return;
        }

        // Resetea todos los estados antes de activar uno nuevo
        ResetAll();

        // Activa el botón actual y su menú
        estado = true;
        if (menu != null)
        {
            menu.SetActive(true);
        }
        boton.imagenBoton.material.mainTexture = boton.spritePresionado;
    }

    // Método para actualizar el estado y el sprite del botón
    private void ActivarDesactivarSinReset(BotonUI boton, GameObject menu, ref bool estado)
    {
        // Si el botón ya está activado, simplemente lo desactiva y devuelve
        if (estado)
        {
            estado = false;
            if (menu != null)
            {
                menu.SetActive(false);
            }
            boton.imagenBoton.material.mainTexture = boton.spriteSinPresionar;
            return;
        }

        // Activa el botón actual y su menú
        estado = true;
        if (menu != null)
        {
            menu.SetActive(true);
        }
        boton.imagenBoton.material.mainTexture = boton.spritePresionado;
    }

    // Métodos públicos para cada acción
    public void ActivarDesactivarPintar()
    {
        ActivarDesactivar(pincel, menuPincel, ref pincelActivado);
    }

    public void ActivarDesactivarBorrar()
    {
        ActivarDesactivar(borrar, null, ref borrarActivado);
    }

    public void ActivarDesactivarFormas()
    {
        ActivarDesactivar(formas, menuFormas, ref formasActivado);
    }

    public void ActivarDesactivarFormaCiruclo_Hololens()
    {
        ActivarDesactivar(circulo, null, ref circuloActivado);
    }
    public void ActivarDesactivarFormaFlecha_Hololens()
    {
        ActivarDesactivar(flecha, null, ref flechaActivado);
    }

    public void ActivarDesactivarPuntero_Hololens()
    {
        ActivarDesactivar(pointer, null, ref pointerActivado);
    }

    public void ActivarDesactivarStreaming()
    {
        ActivarDesactivarSinReset(streaming, null, ref streamingActivado);
    }

    public void ActivarDesactivarFicha()
    {
        //ficha.SetActive(false);
        //escaner.SetActive(false);
        //ingresoRut.SetActive(false);

        ActivarDesactivarSinReset(fichaMedica, ficha, ref fichaActivado);
    }
}
