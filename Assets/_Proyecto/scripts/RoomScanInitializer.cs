using UnityEngine;
using Meta.XR.MRUtilityKit;


public class RoomScanInitializer : MonoBehaviour
{
    void Start()
    {
        OVRScene.RequestSpaceSetup();
    }

    public void Reescan() 
    {
        OVRScene.RequestSpaceSetup();
        Debug.Log("iniciando scaneo");
    }
}
