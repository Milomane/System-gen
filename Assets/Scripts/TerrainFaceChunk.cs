using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class TerrainFaceChunk
{
    private ShapeGenerator shapeGenerator;
    private ColourGenerator colourGenerator;
    private ColoursSettings colourSettings;
    private MeshFilter meshFilter;
    private Mesh mesh;
    private int resolution;
    private Vector3 localUp;
    private Vector3 axisA;
    private Vector3 axisB;

    private int chunkXPos;
    private int chunkYPos;
    private int chunkPerFaceLine;

    private Transform player;
    private TerrainFaceChunkManager parentManager;
    private TerrainFaceChunk[] children;
    private int detailLevel;
    public bool isRendered;
    private bool meshGenerating;
    public bool meshGenerated;
    private bool uvGenerated;

    private Queue<MeshThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MeshThreadInfo<MeshData>>();

    public TerrainFaceChunk(ShapeGenerator shapeGenerator, ColourGenerator colourGenerator, ColoursSettings colourSettings, int resolution, Vector3 localUp, int chunkXPos, int chunkYPos, int chunkPerFaceLine, TerrainFaceChunkManager parentManager, Transform player, int detailLevel)
    {
        this.shapeGenerator = shapeGenerator;
        this.resolution = resolution;
        this.localUp = localUp;
        this.chunkXPos = chunkXPos;
        this.chunkYPos = chunkYPos;
        this.chunkPerFaceLine = chunkPerFaceLine;
        this.parentManager = parentManager;
        this.player = player;
        this.detailLevel = detailLevel;
        this.colourSettings = colourSettings;
        this.colourGenerator = colourGenerator;
        
        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }
    
    public void GenerateChildrens()
    {
        if (detailLevel <= Planet.detailsLevelDistances.Count-1 && detailLevel >= 0)
        {
            if (NeedChildren())
            {
                isRendered = false;
                
                if (children != null)
                {
                    ActiveChildren();
                }
                else
                {
                    children = new TerrainFaceChunk[4];
                    children[0] = new TerrainFaceChunk(shapeGenerator, colourGenerator, colourSettings, resolution, localUp, chunkXPos * 2, chunkYPos * 2, chunkPerFaceLine * 2, parentManager, player, detailLevel+1);
                    children[1] = new TerrainFaceChunk(shapeGenerator, colourGenerator, colourSettings, resolution, localUp, chunkXPos * 2 + 1, chunkYPos * 2, chunkPerFaceLine * 2, parentManager, player, detailLevel+1);
                    children[2] = new TerrainFaceChunk(shapeGenerator, colourGenerator, colourSettings, resolution, localUp, chunkXPos * 2, chunkYPos * 2 + 1, chunkPerFaceLine * 2, parentManager, player, detailLevel+1);
                    children[3] = new TerrainFaceChunk(shapeGenerator, colourGenerator, colourSettings, resolution, localUp, chunkXPos * 2 + 1, chunkYPos * 2 + 1, chunkPerFaceLine * 2, parentManager, player, detailLevel+1);

                    foreach (var child in children)
                    {
                        child.GenerateChildrens();
                    }
                }
            }
            else if (!isRendered)
            {
                isRendered = true;
                DeactivateChildren();
                GenerateObjectAndMeshFilter();
            }
            else
            {
                DeactivateChildren();
            }
        } 
        else if (!isRendered)
        {
            isRendered = true;
            GenerateObjectAndMeshFilter();
        }
    }

    public void DeactivateChildren()
    {
        if (children != null)
        {
            foreach (var child in children)
            {
                child.isRendered = false;
                child.DeactivateChildren();
                if (child.meshFilter != null)
                {
                    if (child.meshFilter.gameObject.activeInHierarchy)
                        child.meshFilter.gameObject.SetActive(false);
                }
            }
        }
    }

    public void ActiveChildren()
    {
        if (children != null)
        {
            foreach (var child in children)
            {
                child.GenerateChildrens();
            }
            
            if (meshFilter != null)
            {
                if (CheckIfChildrenMeshAreGenerated())
                {
                    if (meshFilter.gameObject.activeInHierarchy)
                        meshFilter.gameObject.SetActive(false);
                }
            }
        }
    }

    public bool CheckIfChildrenMeshAreGenerated()
    {
        if (isRendered && meshGenerated)
        {
            return true;
        } else if (!isRendered)
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (!child.CheckIfChildrenMeshAreGenerated())
                        return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public void GenerateObjectAndMeshFilter()
    {
        if (meshFilter == null)
        {
            GameObject meshObj = new GameObject("Mesh[" + chunkXPos + ", " + chunkYPos + "]" + " [LOD:" + detailLevel + "]");
            meshObj.transform.parent = parentManager.transform;

            meshObj.AddComponent<MeshRenderer>();
            meshFilter = meshObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }
        else
        {
            meshFilter.gameObject.SetActive(true);
        }
        
        meshFilter.GetComponent<MeshRenderer>().sharedMaterial = colourSettings.planetMaterial;
    }

    public void ConstructMeshOrChildrenMesh()
    {
        if (NeedChildren())
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    child.ConstructMeshOrChildrenMesh();
                }
            }
            else
            {
                Debug.LogError("Children should be generated but their not");
            }
        }
        else if (!meshGenerated)
        {
            
            if (!meshGenerating)
            {
                ThreadConstructMesh(OnMeshDataReceived);
            }
            else if (meshDataThreadInfoQueue.Count > 0 && meshFilter.gameObject.activeInHierarchy)
            {
                MeshThreadInfo<MeshData> meshThreadInfo = meshDataThreadInfoQueue.Dequeue();
                meshThreadInfo.callback(meshThreadInfo.parameter);
            }
        }
    }

    void ThreadConstructMesh(Action<MeshData> callBack)
    {
        meshGenerating = true;
        ThreadStart threadStart = delegate
        {
            MeshThread(callBack);
        };
        new Thread(threadStart).Start();
    }

    void MeshThread(Action<MeshData> callBack)
    {
        MeshData meshData = ConstructMesh();
        meshDataThreadInfoQueue.Enqueue(new MeshThreadInfo<MeshData>(callBack, meshData));
    }

    public void OnMeshDataReceived(MeshData meshData)
    {
        ApplyMesh(meshData);
    }

    public void ApplyMesh(MeshData meshData)
    {
        mesh.vertices = meshData.vertices;
        mesh.triangles = meshData.triangles;
        mesh.uv = meshData.uv;
        mesh.normals = meshData.normals;

        meshGenerated = true;
    }

    public MeshData ConstructMesh()
    {
        int borderedResolution = resolution + 2;
    
        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector3[] borderVertices = new Vector3[resolution * 4 + 4];
        int[] triangles = new int[(resolution -1) * (resolution-1) * 6];
        int[] borderTriangles = new int[6 * 4 * resolution];
        int triIndex = 0;
        int borderTriIndex = 0;
        Vector2[] uv = new Vector2[vertices.Length];
        Vector3[] normals;
        
        int [,] vertexIndicesMap = new int[borderedResolution, borderedResolution];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;


        for (int y = 0; y < borderedResolution; y++)
        {
            for (int x = 0; x < borderedResolution; x++)
            {
                bool isBorderVertex = y == 0 || y == borderedResolution-1 || x == 0 || x == borderedResolution-1;

                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }
        
        for (int y = 0; y < borderedResolution; y++)
        {
            for (int x = 0; x < borderedResolution; x++)
            {
                int vertexIndex = vertexIndicesMap[x, y];

                Vector3 pointOnUnitySphere = GetPointOnUnitSphere(x, y);
                float unscaledElevation = shapeGenerator.CalcultateUnscaledElevation(pointOnUnitySphere);
                
                if (vertexIndex < 0)
                {
                    borderVertices[-vertexIndex-1] = pointOnUnitySphere * shapeGenerator.GetScaledElevation(unscaledElevation);
                }
                else
                {
                    vertices[vertexIndex] = pointOnUnitySphere * shapeGenerator.GetScaledElevation(unscaledElevation);
                    uv[vertexIndex].y = unscaledElevation;
                }
                
                if (x != borderedResolution - 1 && y != borderedResolution - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + 1, y];
                    int c = vertexIndicesMap[x, y + 1];
                    int d = vertexIndicesMap[x + 1, y + 1];
                    
                    
                    AddTriangle(a, d, c, ref triIndex, ref borderTriIndex, ref triangles, ref borderTriangles);
                    AddTriangle(a, b, d, ref triIndex, ref borderTriIndex, ref triangles, ref borderTriangles);
                }
            }
        }
        
        normals = CalculateNormals(vertices, triangles, borderVertices, borderTriangles);
        
        return new MeshData(vertices, triangles, uv, normals);
    }

    public struct MeshData
    {
        public readonly Vector3[] vertices;
        public readonly int[] triangles;
        public readonly Vector2[] uv;
        public readonly Vector3[] normals;

        public MeshData(Vector3[] vertices, int[] triangles, Vector2[] uv, Vector3[] normals)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.uv = uv;
            this.normals = normals;
        }
    }

    struct MeshThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MeshThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

    void AddTriangle(int a, int b, int c, ref int triIndex, ref int borderTriIndex, ref int[] triangles, ref int[] borderTriangles)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriIndex] = a;
            borderTriangles[borderTriIndex+1] = b;
            borderTriangles[borderTriIndex+2] = c;
            borderTriIndex += 3;
        }
        else
        {
            triangles[triIndex] = a;
            triangles[triIndex+1] = b;
            triangles[triIndex+2] = c;
            triIndex += 3;
        }
    }

    Vector3[] CalculateNormals(Vector3[] vertices, int[] triangles, Vector3[] borderVertices, int[]borderTriangles)
    {
        Vector3[] vertexsNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length/3;

        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex + 0];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC, vertices, borderVertices);
            vertexsNormals[vertexIndexA] += triangleNormal;
            vertexsNormals[vertexIndexB] += triangleNormal;
            vertexsNormals[vertexIndexC] += triangleNormal;
        }
        
        int borderTriangleCount = borderTriangles.Length/3;

        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex + 0];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC, vertices, borderVertices);
            if (vertexIndexA >= 0)
            {
                vertexsNormals[vertexIndexA] += triangleNormal;
            }

            if (vertexIndexB >= 0)
            {
                vertexsNormals[vertexIndexB] += triangleNormal;
            }

            if (vertexIndexC >= 0)
            {
                vertexsNormals[vertexIndexC] += triangleNormal;
            }
        }

        for (int i = 0; i < vertexsNormals.Length; i++)
        {
            vertexsNormals[i].Normalize();
        }

        return vertexsNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC, Vector3[] vertices, Vector3[] borderVertices)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA-1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB-1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC-1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void UpdateUVsOrChildrenUvs(ColourGenerator colourGenerator)
    {
        if (NeedChildren())
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    child.UpdateUVsOrChildrenUvs(colourGenerator);
                }
            }
            else
            {
                Debug.LogError("Children should be generated but their not");
            }
        }
        else
        {
            UpdateUVs(colourGenerator);
        }
    }

    public void UpdateUVs(ColourGenerator colourGenerator)
    {
        if (!uvGenerated && meshGenerated)
        {
            Vector2[] uv = mesh.uv;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = x + y * resolution;
                    Vector2 percent = (new Vector2(x-1 + chunkXPos * (resolution-1), y-1 + chunkYPos * (resolution-1)) / (resolution - 1)) / chunkPerFaceLine;
                    Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                    Vector3 pointOnUnitySphere = pointOnUnitCube.normalized;

                    uv[i].x = colourGenerator.BiomePercentFromPoint(pointOnUnitySphere);
                }
            }

            mesh.uv = uv;
            uvGenerated = true;
        }
    }

    public Vector3 GetPointOnUnitSphere(int x, int y)
    {
        Vector2 percent = (new Vector2(x-1 + chunkXPos * (resolution-1), y-1 + chunkYPos * (resolution-1)) / (resolution - 1)) / chunkPerFaceLine;
        Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
        Vector3 pointOnUnitySphere = pointOnUnitCube.normalized;

        return pointOnUnitySphere;
    }

    public bool NeedChildren()
    {
        if (detailLevel < Planet.detailsLevelDistances.Count - 1)
        {
            float distance = Vector3.Distance(
                GetPointOnUnitSphere((resolution - 1) / 2, (resolution - 1) / 2) *
                shapeGenerator.settings.planetRadius, player.position);
            return (distance <= Planet.detailsLevelDistances[detailLevel] * shapeGenerator.settings.planetRadius);
        }
        else
        {
            return false;
        }
    }
}
