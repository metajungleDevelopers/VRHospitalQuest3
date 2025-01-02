using FMSolution.FMETP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class TestsManager : MonoBehaviour
{
    [SerializeField] private GameObject encoderStuff;
    [SerializeField] private GameObject decoderStuff;
    [SerializeField] private GameViewEncoder gameViewEncoder;
    [SerializeField] private GameObject dissonance;
    [SerializeField] private Vector2 resolution;
    [SerializeField] private InputField resolutionScaleX;
    [SerializeField] private InputField resolutionScaleY;
    [SerializeField] private Slider sliderCalidad;

    public void PrenderApagarTransmisionEnviadaDeHololens()
    {
        encoderStuff.SetActive(!encoderStuff.activeSelf);
        gameViewEncoder.enabled = !gameViewEncoder.enabled;
    }

    public void PrenderApagarTransmisionRecibida()
    {
        decoderStuff.SetActive(!decoderStuff.activeSelf);
    }

    public void PrenderApagarFuncionalidades()
    {

    }

    public void PrenderApagarVoz()
    {
        dissonance.SetActive(!dissonance.activeSelf);
    }

    public void AjustarResolucionDeTransmision()
    {
        int.TryParse(resolutionScaleX.text, out int x);
        int.TryParse(resolutionScaleY.text, out int y);

        gameViewEncoder.Resolution = new Vector2(x, y);
    }

    public void AjustarCalidadDeTransmision()
    {
        gameViewEncoder.Quality = (int)sliderCalidad.value;
    }
}
