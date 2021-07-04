using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFace
{
    private ShapeGenerator shapeGenerator;
    private Mesh mesh;
    private int resolution;
    private Vector3 localUp;
    private Vector3 axisA;
    private Vector3 axisB;

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp)
    {
        this.shapeGenerator = shapeGenerator;
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;
        
        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    public void ConstructMesh()
    {
        int borderedResolution = resolution + 2; // Here
        
        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector3[] borderVertices = new Vector3[resolution * 4 + 4]; // Here
        int[] triangles = new int[(resolution -1) * (resolution-1) * 6];
        int[] borderTriangles = new int[6 * 4 * resolution]; // Here
        int triIndex = 0;
        int borderTriIndex = 0; // Here
        Vector2[] uv = (mesh.uv.Length == vertices.Length)?mesh.uv : new Vector2[vertices.Length];
        
        // Here
        int [,] vertexIndicesMap = new int[borderedResolution, borderedResolution];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;


        // Here
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
                
                // Create vertices
                Vector2 percent = new Vector2(x-1, y-1) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitySphere = pointOnUnitCube.normalized;
                float unscaledElevation = shapeGenerator.CalcultateUnscaledElevation(pointOnUnitySphere);
                
                // Add the vertex to the array
                if (vertexIndex < 0)
                {
                    borderVertices[-vertexIndex-1] = pointOnUnitySphere * shapeGenerator.GetScaledElevation(unscaledElevation);
                }
                else
                {
                    vertices[vertexIndex] = pointOnUnitySphere * shapeGenerator.GetScaledElevation(unscaledElevation);
                    uv[vertexIndex].y = unscaledElevation;
                }
                
                // Warning here
                if (x != borderedResolution - 1 && y != borderedResolution - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + 1, y];
                    int c = vertexIndicesMap[x, y + 1];
                    int d = vertexIndicesMap[x + 1, y + 1];
                    
                    // Create triangle
                    AddTriangle(a, d, c, ref triIndex, ref borderTriIndex, ref triangles, ref borderTriangles);
                    AddTriangle(a, b, d, ref triIndex, ref borderTriIndex, ref triangles, ref borderTriangles);
                }
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = CalculateNormals(vertices, triangles, borderVertices, borderTriangles);
        
        if (mesh.uv.Length == uv.Length)
            mesh.uv = uv;
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

    public void UpdateUVs(ColourGenerator colourGenerator)
    {
        Vector2[] uv = mesh.uv;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitySphere = pointOnUnitCube.normalized;

                uv[i].x = colourGenerator.BiomePercentFromPoint(pointOnUnitySphere);
            }
        }

        mesh.uv = uv;
    }
}
