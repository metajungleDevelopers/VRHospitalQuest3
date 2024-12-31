using Photon.Pun;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class OwnerOnStart : MonoBehaviourPunCallbacks
{
    public bool isVR;

    // La ultima prueba que me funciono la rotacion de la CameraNetworkTransform era 5, 0, 0
    private Quaternion startRotation;

    private void Awake()
    {
        startRotation = transform.rotation;
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        if (isVR)
        {
            // Transferir ownership al jugador local cuando el objeto es agarrado
            photonView.RequestOwnership();
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();   
        if(!photonView.IsMine && isVR)
        {
            StartCoroutine(AsegurarOwnership());
        }
    }

    IEnumerator AsegurarOwnership()
    {
        while(!photonView.IsMine)
        {
            Debug.Log("AAAAAAAAAAAAAAAAAAAA");
            photonView.RequestOwnership();
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
            yield return new WaitForSeconds(1);
        }
    }

    //Este es codigo de prueba solamente y no servira para hacer la funcionalidad bidireccional
    public void KeepZeroRotation()
    {
        if (isVR)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = startRotation;
        }
    }

    private void Update()
    {
        KeepZeroRotation();
    }
}
