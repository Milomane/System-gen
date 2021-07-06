using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFaceChunkManager : MonoBehaviour
{
    private ShapeGenerator shapeGenerator;
    private int resolution;
    private Vector3 localUp;
    private int chunkPerFaceLine;

    private TerrainFaceChunk chunkParent;

    private Transform player;
    private ColoursSettings colourSettings;

    public void Initialize(ShapeGenerator shapeGenerator, ColourGenerator colourGenerator, int resolution, Vector3 localUp, int chunkPerFaceLine, ColoursSettings colourSettings, Transform player)
    {
        this.shapeGenerator = shapeGenerator;
        this.resolution = resolution;
        this.localUp = localUp;
        this.chunkPerFaceLine = chunkPerFaceLine;
        this.player = player;
        this.colourSettings = colourSettings;
        
        ConstructTree(colourGenerator);
    }

    public void ConstructAllMeshs()
    {
        chunkParent.ConstructMeshOrChildrenMesh();
    }

    public void UpdateAllUVs(ColourGenerator colourGenerator)
    {
        chunkParent.UpdateUVsOrChildrenUvs(colourGenerator);
    }

    public void ConstructTree(ColourGenerator colourGenerator)
    {
        chunkParent = new TerrainFaceChunk(shapeGenerator, colourGenerator, colourSettings, resolution, localUp, 0, 0, chunkPerFaceLine, this, player, 0);
        chunkParent.GenerateChildrens();
    }

    public void UpdateChildren(ColourGenerator colourGenerator)
    {
        chunkParent.GenerateChildrens();
        chunkParent.ConstructMeshOrChildrenMesh();
        chunkParent.UpdateUVsOrChildrenUvs(colourGenerator);
    }
}
