using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIStatsRadarChart : MonoBehaviour
{
    public static UIStatsRadarChart instance;
    private Stats stats;
    [SerializeField]
    private CanvasRenderer radarMeshCanvasRenderer;
    [SerializeField]
    private Material radarMaterial;
    [SerializeField]
    private Texture2D radarTexture;
    private Mesh mesh;

    private void Awake()
    {
        instance = this;
    }
    public void SetStats(Stats stats)
    {
        this.stats = stats;
        UpdateStatsVisual();
    }

    public void ClearMesh()
    {
        radarMeshCanvasRenderer.Clear();
    }

    private void UpdateStatsVisual()
    {
        mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[3 * 3];

        float radarChartSize = 163f;
        float angleIncrement = -120f;

        Vector3 attackVertex = Quaternion.Euler(0, 0, angleIncrement * 0) * Vector3.up * radarChartSize * stats.GetStatAmountNormalized(Stats.Type.Attack);
        int attackVertexIndex = 1;

        Vector3 armorVertex = Quaternion.Euler(0, 0, angleIncrement * 1) * Vector3.up * radarChartSize * stats.GetStatAmountNormalized(Stats.Type.Armor);
        int armorVertexIndex = 2;

        Vector3 speedVertex = Quaternion.Euler(0, 0, angleIncrement * 2) * Vector3.up * radarChartSize * stats.GetStatAmountNormalized(Stats.Type.Speed);
        int speedVertexIndex = 3;

        vertices[0] = Vector3.zero;
        vertices[attackVertexIndex] = attackVertex;
        vertices[armorVertexIndex] = armorVertex;
        vertices[speedVertexIndex] = speedVertex;

        uv[0] = Vector2.zero;
        uv[attackVertexIndex] = Vector2.one;
        uv[armorVertexIndex] = Vector2.one;
        uv[speedVertexIndex] = Vector2.one;

        triangles[0] = 0;
        triangles[1] = attackVertexIndex;
        triangles[2] = armorVertexIndex;

        triangles[3] = 0;
        triangles[4] = armorVertexIndex;
        triangles[5] = speedVertexIndex;

        triangles[6] = 0;
        triangles[7] = speedVertexIndex;
        triangles[8] = attackVertexIndex;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        radarMeshCanvasRenderer.SetMesh(mesh);
        radarMeshCanvasRenderer.SetMaterial(radarMaterial, radarTexture);
    }
}
