using MetaJungle.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorHandler : SingletonMonoBehaviourPunCallbacks<ColorHandler>
{
    public Color[] paintColorPallete; // Paleta de colores
    public int currentColorIndex; // Índice del color actual
    public Color paintColor = Color.gray; // Color de pintura por defecto

    public void ChangePaintColor(int colorIndex)
    {
        currentColorIndex = colorIndex;
        paintColor = paintColorPallete[colorIndex];
    }
}
