using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Planet))]
public class PlanetEditor : Editor
{
    private Planet planet;
    private Editor shapeEditor;
    private Editor colourEditor;

    public override void OnInspectorGUI()
    {
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            base.OnInspectorGUI();
            if (check.changed)
            {
                planet.GeneratePlanet();
            }
        }

        if (GUILayout.Button("Generate Planet"))
        {
            planet.GeneratePlanet();
        }

        DrawSettingsEditor(planet.shapeSettings, planet.OnShapeSettingsUpdated, ref planet.shapeSettingsFoldout, ref shapeEditor);
        DrawSettingsEditor(planet.colourSettings, planet.OnShapeSettingsUpdated, ref planet.colourSettingsFoldout, ref colourEditor);
    }

    void DrawSettingsEditor(ScriptableObject settings, System.Action onSettingsUpdated, ref bool foldOut, ref Editor editor)
    {
        if (settings != null)
        {
            foldOut = EditorGUILayout.InspectorTitlebar(foldOut, settings);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (foldOut)
                {
                    CreateCachedEditor(settings, null, ref editor);
                    editor.OnInspectorGUI();

                    if (check.changed)
                    {
                        if (onSettingsUpdated != null)
                        {
                            onSettingsUpdated();
                        }
                    }
                }
            }
        }
    }
    
    private void OnEnable()
    {
        planet = (Planet) target;
    }
}
