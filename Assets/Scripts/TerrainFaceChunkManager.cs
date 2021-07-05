using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFaceChunkManager : MonoBehaviour
{
    private ShapeGenerator shapeGenerator;
    private int resolution;
    private Vector3 localUp;
    private int chunkPerFaceLine;

    private TerrainFaceChunk[,] chunks;
    [SerializeField, HideInInspector] 
    private MeshFilter[,] meshFilters;

    public void Initialize(ShapeGenerator shapeGenerator, int resolution, Vector3 localUp, int chunkPerFaceLine, ColoursSettings colourSettings)
    {
        this.shapeGenerator = shapeGenerator;
        this.resolution = resolution;
        this.localUp = localUp;
        this.chunkPerFaceLine = chunkPerFaceLine;

        
        if (meshFilters == null || meshFilters.Length == 0)
        {
            meshFilters = new MeshFilter[chunkPerFaceLine,chunkPerFaceLine];
        }
        else
        {
            int mfLenght = meshFilters.GetLength(0);
            if (mfLenght != chunkPerFaceLine)
            {
                for (int y = 0; y < mfLenght; y++)
                {
                    for (int x = 0; x < mfLenght; x++)
                    {
                        if (meshFilters[x, y] != null)
                            DestroyImmediate(meshFilters[x, y].gameObject);
                    }
                }
                
                meshFilters = new MeshFilter[chunkPerFaceLine,chunkPerFaceLine];
            }
        }

        if (chunks == null || chunks.Length == 0)
        {
            chunks = new TerrainFaceChunk[chunkPerFaceLine,chunkPerFaceLine];
        }
        else
        {
            int cLenght = chunks.GetLength(0);
            if (cLenght != chunkPerFaceLine)
            {
                chunks = new TerrainFaceChunk[chunkPerFaceLine,chunkPerFaceLine];
            }
        }
        
        for (int y = 0; y < chunkPerFaceLine; y++)
        {
            for (int x = 0; x < chunkPerFaceLine; x++)
            {
                if (meshFilters[x, y] == null)
                {
                    GameObject meshObj = new GameObject("Mesh[" + x + ", " + y + "]");
                    meshObj.transform.parent = transform;

                    meshObj.AddComponent<MeshRenderer>();
                    meshFilters[x, y] = meshObj.AddComponent<MeshFilter>();
                    meshFilters[x, y].sharedMesh = new Mesh();
                }
                
                meshFilters[x, y].GetComponent<MeshRenderer>().sharedMaterial = colourSettings.planetMaterial;
                
                chunks[x, y] = new TerrainFaceChunk(shapeGenerator, meshFilters[x, y].sharedMesh, resolution, localUp, x, y, chunkPerFaceLine);
            }
        }
    }

    public void ConstructAllMeshs()
    {
        for (int y = 0; y < chunkPerFaceLine; y++)
        {
            for (int x = 0; x < chunkPerFaceLine; x++)
            {
                
                chunks[x, y].ConstructMesh();
            }
        }
    }

    public void UpdateAllUVs(ColourGenerator colourGenerator)
    {
        for (int y = 0; y < chunkPerFaceLine; y++)
        {
            for (int x = 0; x < chunkPerFaceLine; x++)
            {
                chunks[x, y].UpdateUVs(colourGenerator);
            }
        }
    }
}
