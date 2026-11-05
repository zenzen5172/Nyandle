using System.Collections.Generic;
using UnityEngine;

// 火の弓矢のクールダウン（intervalArrow の残り時間）を、Playerの頭上に円グラフで表示する
// 残り時間が0のときは何も表示しない
// Playerと同じGameObjectに付けるだけで動く（表示用オブジェクトは自動生成）
[RequireComponent(typeof(Player))]
public class ArrowCooldownUI : MonoBehaviour
{
    [Header("表示位置・大きさ")]
    public Vector2 offset = new Vector2(0f, 1.2f); // Playerの位置からのずれ（頭上）
    public float radius = 0.25f;                    // 円の半径
    [Range(0.5f, 1f)] public float fillScale = 0.85f; // 背景の円に対する中身（扇形）の大きさ
    [Range(8, 128)] public int segments = 48;       // 円の滑らかさ

    [Header("色")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);
    public Color fillColor = new Color(1f, 0.55f, 0.1f, 0.9f);

    [Header("描画順（他のスプライトより手前に出す）")]
    public string sortingLayerName = "Player"; // "Default"は背景より奥に描かれるので不可
    public int sortingOrder = 100;

    Player player;
    GameObject root;
    Mesh mesh;
    Material material;
    float lastRatio = -1f;

    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Color> colors = new List<Color>();
    readonly List<int> triangles = new List<int>();

    void Awake()
    {
        player = GetComponent<Player>();

        // Playerの子にすると左右反転（scale.x = -1）で円グラフの向きまで反転するので、親子にせず位置だけ追従させる
        root = new GameObject("ArrowCooldownUI");
        mesh = new Mesh { name = "ArrowCooldownPie" };
        mesh.MarkDynamic();
        root.AddComponent<MeshFilter>().sharedMesh = mesh;

        material = new Material(Shader.Find("Sprites/Default"));
        MeshRenderer mr = root.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.sortingLayerName = sortingLayerName;
        mr.sortingOrder = sortingOrder;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        root.SetActive(false);
    }

    void LateUpdate()
    {
        float ratio = player.IntervalArrowRatio;

        if (ratio <= 0f)
        {
            if (root.activeSelf) root.SetActive(false);
            return;
        }

        if (!root.activeSelf) root.SetActive(true);

        Vector3 p = transform.position;
        root.transform.position = new Vector3(p.x + offset.x, p.y + offset.y, p.z);

        if (!Mathf.Approximately(ratio, lastRatio))
        {
            Rebuild(ratio);
            lastRatio = ratio;
        }
    }

    void OnDisable()
    {
        if (root != null) root.SetActive(false);
    }

    void OnDestroy()
    {
        if (root != null) Destroy(root);
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }

    // 背景の円 ＋ 残り時間ぶんの扇形（時計の針のように時計回りに減っていき、12時の位置で0になる）
    void Rebuild(float ratio)
    {
        vertices.Clear();
        colors.Clear();
        triangles.Clear();

        AddFan(1f, radius, backgroundColor);
        AddFan(ratio, radius * fillScale, fillColor); // 後に追加した三角形が手前に描かれる

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    void AddFan(float fraction, float r, Color color)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(segments * fraction));
        int center = vertices.Count;

        vertices.Add(Vector3.zero);
        colors.Add(color);

        for (int i = 0; i <= steps; i++)
        {
            // 90°（真上）から反時計回りに fraction ぶん広げる。
            // fraction が減ると端（90° + 360°×fraction）が時計の針のように時計回りに進み、扇形が減っていく
            float angle = (90f + 360f * fraction * i / steps) * Mathf.Deg2Rad;
            vertices.Add(new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
            colors.Add(color);
        }

        for (int i = 0; i < steps; i++)
        {
            // 頂点が反時計回りに並ぶので、三角形は逆順にして時計回り（カメラから見て表）にする
            triangles.Add(center);
            triangles.Add(center + 2 + i);
            triangles.Add(center + 1 + i);
        }
    }
}
