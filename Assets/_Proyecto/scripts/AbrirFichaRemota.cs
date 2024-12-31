using Photon.Pun;
using UnityEngine;

public class AbrirFichaRemota : MonoBehaviourPunCallbacks
{
    public GameObject ficha;
    public GameObject ingresoRut;
    public GameObject dicom;

    public void AbrirFicha()
    {
        photonView.RPC(nameof(AbrirFicha_RPC), RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void AbrirFicha_RPC()
    {
        ficha.SetActive(true);
        dicom.SetActive(true);
        if (ingresoRut != null)
        {
            ingresoRut.SetActive(false);
        }
    }

    public void ApagarFicha()
    {
        photonView.RPC(nameof(ApagarFicha_RPC), RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void ApagarFicha_RPC()
    {
        ficha.SetActive(false);
        dicom.SetActive(false);
        if (ingresoRut != null && ingresoRut.activeSelf)
        {
            ingresoRut.SetActive(false);
        }
    }

    public void PrenderDICOM()
    {
        photonView.RPC(nameof(PrenderDICOM_RPC), RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void PrenderDICOM_RPC()
    {
        dicom.SetActive(true);
    }

    public void ApagarDICOM()
    {
        photonView.RPC(nameof(ApagarDICOM_RPC), RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void ApagarDICOM_RPC()
    {
        dicom.SetActive(false);
    }
}
