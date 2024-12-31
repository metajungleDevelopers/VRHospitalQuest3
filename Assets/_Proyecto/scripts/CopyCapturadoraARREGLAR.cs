using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CopyCapturadoraARREGLAR : MonoBehaviour
{
    public RawImage rawImageCapturadora;
    public RawImage rawImageCopia;

    private void Update()
    {
        rawImageCopia.gameObject.SetActive(rawImageCapturadora.gameObject.activeInHierarchy);

        if(rawImageCopia.texture != rawImageCapturadora.texture)
        {
            rawImageCopia.texture = rawImageCapturadora.texture;
        }
    }
}
