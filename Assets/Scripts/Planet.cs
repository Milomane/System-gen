using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [Range(2, 255)] 
    public int resolution = 10;
    [Range(1, 255)]
    public int chunkPerFaceLine = 16;
    public bool autoUpdate = true;
    public enum FaceRenderMask {All, Top, Bottom, Left, Right, Front, Back}

    public FaceRenderMask faceRenderMask;

    public ShapeSettings shapeSettings;
    public ColoursSettings colourSettings;

    [HideInInspector]
    public bool shapeSettingsFoldout;
    [HideInInspector]
    public bool colourSettingsFoldout;

    private ShapeGenerator shapeGenerator = new ShapeGenerator();
    private ColourGenerator colourGenerator = new ColourGenerator();
    
    [SerializeField, HideInInspector]
    private TerrainFaceChunkManager[] terrainFacesChunkManager;

    public Transform player;

    public static Dictionary<int, float> detailsLevelDistances = new Dictionary<int, float>()
    {
        {0, 1f},
        {1, .75f},
        {2, .6f},
        {3, .4f},
        {4, .2f},
        {5, .125f},
        {6, .075f}
    };

    void Initialize()
    {
        shapeGenerator.UpdateSettings(shapeSettings);
        colourGenerator.UpdateSettings(colourSettings);

        if (terrainFacesChunkManager == null || terrainFacesChunkManager.Length == 0)
        {
            terrainFacesChunkManager = new TerrainFaceChunkManager[6];
        }
        
        Vector3[] directions = {Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back};

        for (int i = 0; i < 6; i++)
        {
            if (terrainFacesChunkManager[i] == null || terrainFacesChunkManager.Length == 0)
            {
                var enumDisplayName = (FaceRenderMask) i+1;
                string chunkManagerName = enumDisplayName.ToString();
                GameObject chunkManagerObj = new GameObject("ChunkManager " + chunkManagerName);
                chunkManagerObj.transform.parent = transform;

                terrainFacesChunkManager[i] = chunkManagerObj.AddComponent<TerrainFaceChunkManager>();
            }
            
            terrainFacesChunkManager[i].Initialize(shapeGenerator, colourGenerator, resolution, directions[i], chunkPerFaceLine, colourSettings, player);
            
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int) faceRenderMask - 1 == i;
            terrainFacesChunkManager[i].gameObject.SetActive(renderFace);
        }
    }

    public void GeneratePlanet()
    {
        foreach (var tcm in terrainFacesChunkManager)
        {
            if (tcm != null)
                DestroyImmediate(tcm.gameObject);
        }
        
        Initialize();
        GenerateMesh();
        GenerateColours();
    }

    public void OnShapeSettingsUpdated()
    {
        if (autoUpdate)
        {
            Initialize();
            GenerateMesh();
        }
    }
    
    public void OnColourSettingsUpdated()
    {
        if (autoUpdate)
        {
            Initialize();
            GenerateColours();
        }
    }

    void GenerateMesh()
    {
        for (int i = 0; i < 6; i++)
        {
            if (terrainFacesChunkManager[i].gameObject.activeSelf)
            {
                terrainFacesChunkManager[i].ConstructAllMeshs();
            }
        }
        
        colourGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
    }

    void GenerateColours()
    {
        colourGenerator.UpdateColours();
        
        for (int i = 0; i < 6; i++)
        {
            if (terrainFacesChunkManager[i].gameObject.activeSelf)
            {
                terrainFacesChunkManager[i].UpdateAllUVs(colourGenerator);
            }
        }
    }

    public void UpdateLoD()
    {
        for (int i = 0; i < 6; i++)
        {
            if (terrainFacesChunkManager[i].gameObject.activeSelf)
            {
                terrainFacesChunkManager[i].UpdateChildren(colourGenerator);
            }
        }
    }

    private void Start()
    {
        Initialize();
        GenerateMesh();
        GenerateColours();
        
        //InvokeRepeating("UpdateLoD", 0f, 1f);
    }

    void Update()
    {
        UpdateLoD();
    }
}
