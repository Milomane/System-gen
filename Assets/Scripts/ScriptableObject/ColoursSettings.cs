using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ColoursSettings : ScriptableObject
{
    public Gradient textureGradient;
    public Gradient smoothnessGradient;
    public Material planetMaterial;

    public class BiomeColourSettings
    {
        public class Biome
        {
            public Gradient gradient;
        }
    }
}
