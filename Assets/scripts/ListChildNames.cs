using System.Text;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using Photon.Pun;

public class ListChildNames : MonoBehaviour
{
    // Referencia al TextMeshPro donde se mostrarán los nombres
    [SerializeField] private TextMeshProUGUI textMeshPro;
    public GameObject obj;
    public ColliderSender colliderSender;

    private void Start()
    {
        Invoke("encontrar", 10f);
    }

    public void encontrar() 
    {
        obj = GameObject.Find("PREFAB MESH");
        if (obj != null)
        {
            obj.AddComponent<PhotonView>();
            obj.AddComponent<ColliderSender>();
            colliderSender = obj.GetComponent<ColliderSender>();
            colliderSender.OpenXRSpatialMeshObserver = obj.gameObject.transform;
            textMeshPro.text = "se le asigno el script";
        }
    }
}
