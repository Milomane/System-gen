using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourGenerator
{
    private ColoursSettings settings;
    private Texture2D texture;
    private Texture2D smoothnessTexture;
    private const int textureResolution = 150;

    public void UpdateSettings(ColoursSettings settings)
    {
        this.settings = settings;
        if (texture == null)
        {
            texture = new Texture2D(textureResolution, 1);
        }
        if (smoothnessTexture == null)
        {
            smoothnessTexture = new Texture2D(textureResolution, 1);
        }
    }

    public void UpdateElevation(MinMax elevationMinMax)
    {
        settings.planetMaterial.SetVector("_elevationMinMax", new Vector4(elevationMinMax.Min, elevationMinMax.Max));
    }

    public void UpdateColours()
    {
        Color[] textureColours = new Color[textureResolution];
        Color[] smoothnessColours = new Color[textureResolution];
        for (int i = 0; i < textureResolution; i++)
        {
            textureColours[i] = settings.textureGradient.Evaluate(i / (textureResolution - 1f));
            smoothnessColours[i] = settings.smoothnessGradient.Evaluate(i / (textureResolution - 1f));
        }
        texture.SetPixels(textureColours);
        texture.Apply();
        
        smoothnessTexture.SetPixels(smoothnessColours);
        smoothnessTexture.Apply();
        
        settings.planetMaterial.SetTexture("_texture", texture);
        settings.planetMaterial.SetTexture("_smoothnessTexture", smoothnessTexture);
    }
}
