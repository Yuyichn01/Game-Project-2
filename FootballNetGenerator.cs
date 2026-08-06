using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 足球门网程序化生成器
/// 在编辑器中调整参数后自动生成网格，搭配 FootballNet Shader 实现镂空白色网效果
/// 
/// 使用方式：
/// 1. 创建一个空 GameObject，添加此脚本
/// 2. 调整 Width/Height/Depth/TopDepth 匹配你的足球框尺寸
/// 3. 右键脚本标题 → "Generate Mesh" 手动重建
/// 4. 材质会自动创建并应用 FootballNet Shader
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FootballNetGenerator : MonoBehaviour
{
    [Header("网格尺寸 (单位: 米)")]
    [Tooltip("球门宽度（左右）")]
    public float width = 7.32f;

    [Tooltip("球门高度（上下）")]
    public float height = 2.44f;

    [Tooltip("底部网深（球门底部向后延伸距离）")]
    public float depth = 2.0f;

    [Tooltip("顶部网深（横梁向后延伸距离，小于 depth 则形成梯形）")]
    public float topDepth = 1.5f;

    [Header("网格细节")]
    [Tooltip("每面的细分段数，值越大网越软（可做布料物理），值越小性能越好")]
    [Range(1, 32)]
    public int segments = 4;

    [Header("材质设置")]
    [Tooltip("网的颜色")]
    public Color netColor = Color.white;

    [Tooltip("网孔大小（世界单位）")]
    [Range(0.02f, 1.0f)]
    public float cellSize = 0.12f;

    [Tooltip("网线粗细（世界单位）")]
    [Range(0.001f, 0.05f)]
    public float wireThickness = 0.012f;

    [Header("面开关")]
    [Tooltip("是否生成后面网")]
    public bool enableBack = true;

    [Tooltip("是否生成顶面网")]
    public bool enableTop = true;

    [Tooltip("是否生成左侧网")]
    public bool enableLeft = true;

    [Tooltip("是否生成右侧网")]
    public bool enableRight = true;

    [Tooltip("是否生成底面网")]
    public bool enableBottom = false;

    // ---- 内部状态 ----
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Material _material;
    private Mesh _mesh;

    // 缓存的参数，用于检测是否需要重建
    private float _cachedWidth;
    private float _cachedHeight;
    private float _cachedDepth;
    private float _cachedTopDepth;
    private int _cachedSegments;
    private bool _cachedBack, _cachedTop, _cachedLeft, _cachedRight, _cachedBottom;

    private const string ShaderName = "Custom/FootballNet";

    private void OnEnable()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        EnsureMaterial();
        RebuildIfNeeded();
    }

    private void OnValidate()
    {
        // 延迟调用避免在 Inspector 输入过程中频繁重建
        if (gameObject.activeInHierarchy)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && gameObject != null)
                    RebuildIfNeeded();
            };
#endif
        }
    }

    private void EnsureMaterial()
    {
        if (_meshRenderer == null) return;

        // 如果已有材质且 shader 正确，无需重建
        if (_material != null && _material.shader != null && _material.shader.name == ShaderName)
            return;

        // 尝试从 Renderer 获取已有材质
        _material = _meshRenderer.sharedMaterial;
        if (_material != null && _material.shader != null && _material.shader.name == ShaderName)
            return;

        // 查找 Shader（带重试，因为 Unity 可能在重新编译 Shader）
        TryLoadShaderAndCreateMaterial();
    }

    private void TryLoadShaderAndCreateMaterial(int retryCount = 0)
    {
        Shader shader = Shader.Find(ShaderName);

        if (shader == null)
        {
            if (retryCount < 5)
            {
#if UNITY_EDITOR
                // Shader 可能还在编译中，延迟重试
                Debug.Log($"[FootballNetGenerator] Shader 尚未就绪，0.5 秒后重试... ({retryCount + 1}/5)");
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                        TryLoadShaderAndCreateMaterial(retryCount + 1);
                };
#endif
            }
            else
            {
                Debug.LogWarning($"[FootballNetGenerator] 找不到 Shader '{ShaderName}'。" +
                    "\n可能原因: 1) Shader 文件有编译错误（Console 窗口查看红色报错）" +
                    " 2) Shader 文件路径不对（应该在 Assets/Shader/FootballNetShader.shader）" +
                    "\n修复后右键脚本 → 'Fix Material' 重新加载。");
            }
            return;
        }

        if (_material != null)
        {
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
        }

        _material = new Material(shader);
        _material.name = "FootballNet_Mat";
        _meshRenderer.sharedMaterial = _material;
        ApplyMaterialProperties();
        Debug.Log("[FootballNetGenerator] Shader 加载成功，材质已创建。");
    }

    /// <summary>
    /// 手动修复材质（当 Shader 编译错误修复后调用）
    /// </summary>
    [ContextMenu("Fix Material")]
    public void FixMaterial()
    {
        // 强制清除缓存状态，重新加载
        _material = null;
        if (_meshRenderer != null)
        {
            _meshRenderer.sharedMaterial = null;
        }
        EnsureMaterial();
        ApplyMaterialProperties();
    }

    private void ApplyMaterialProperties()
    {
        if (_material == null) return;
        _material.SetColor("_Color", netColor);
        _material.SetFloat("_CellSize", cellSize);
        _material.SetFloat("_WireThickness", wireThickness);
    }

    private bool ParametersChanged()
    {
        return !Mathf.Approximately(_cachedWidth, width)
            || !Mathf.Approximately(_cachedHeight, height)
            || !Mathf.Approximately(_cachedDepth, depth)
            || !Mathf.Approximately(_cachedTopDepth, topDepth)
            || _cachedSegments != segments
            || _cachedBack != enableBack
            || _cachedTop != enableTop
            || _cachedLeft != enableLeft
            || _cachedRight != enableRight
            || _cachedBottom != enableBottom;
    }

    private void RebuildIfNeeded()
    {
        if (ParametersChanged())
            GenerateMesh();
    }

    /// <summary>
    /// 在编辑器中右键菜单手动重建网格
    /// </summary>
    [ContextMenu("Generate Mesh")]
    public void GenerateMesh()
    {
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();
        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();

        EnsureMaterial();
        ApplyMaterialProperties();

        // 清理旧网格
        if (_mesh != null)
        {
            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
        }

        _mesh = new Mesh();
        _mesh.name = "FootballNet_Mesh";

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();

        float halfW = width * 0.5f;
        float botZ = -depth;
        float topZ = -topDepth;

        // ---- 后面 ----
        if (enableBack)
        {
            // 后面是一个四边形：顶点坐标与顶部和底部的深度有关
            // 顶部边缘：(-halfW, height, topZ) 到 (halfW, height, topZ)
            // 底部边缘：(-halfW, 0, botZ) 到 (halfW, 0, botZ)
            AddGridFace(vertices, triangles, normals, uvs,
                new Vector3(-halfW, height, topZ),   // top-left
                new Vector3(halfW, height, topZ),    // top-right
                new Vector3(-halfW, 0, botZ),        // bottom-left
                new Vector3(halfW, 0, botZ),         // bottom-right
                Vector3.back, // 法线向后（面朝前方可见）
                segments, segments);
        }

        // ---- 顶面 ----
        if (enableTop)
        {
            // 顶面是一个梯形：
            // 前边缘：(-halfW, height, 0) 到 (halfW, height, 0)
            // 后边缘：(-halfW, height, topZ) 到 (halfW, height, topZ)
            AddGridFace(vertices, triangles, normals, uvs,
                new Vector3(-halfW, height, 0),      // front-left
                new Vector3(halfW, height, 0),       // front-right
                new Vector3(-halfW, height, topZ),   // back-left
                new Vector3(halfW, height, topZ),    // back-right
                Vector3.down, // 法线向下（面朝上方可见）
                segments, segments);
        }

        // ---- 左侧面 ----
        if (enableLeft)
        {
            // 前边：(-halfW, height, 0) 到 (-halfW, 0, 0)
            // 后边：(-halfW, height, topZ) 到 (-halfW, 0, botZ)
            AddGridFace(vertices, triangles, normals, uvs,
                new Vector3(-halfW, height, 0),      // front-top
                new Vector3(-halfW, 0, 0),           // front-bottom
                new Vector3(-halfW, height, topZ),   // back-top
                new Vector3(-halfW, 0, botZ),        // back-bottom
                Vector3.left,  // 法线向左（从右侧可见）
                segments, segments);
        }

        // ---- 右侧面 ----
        if (enableRight)
        {
            AddGridFace(vertices, triangles, normals, uvs,
                new Vector3(halfW, height, 0),       // front-top
                new Vector3(halfW, 0, 0),            // front-bottom
                new Vector3(halfW, height, topZ),    // back-top
                new Vector3(halfW, 0, botZ),         // back-bottom
                Vector3.right, // 法线向右（从左侧可见）
                segments, segments);
        }

        // ---- 底面（可选） ----
        if (enableBottom)
        {
            AddGridFace(vertices, triangles, normals, uvs,
                new Vector3(-halfW, 0, 0),           // front-left
                new Vector3(halfW, 0, 0),            // front-right
                new Vector3(-halfW, 0, botZ),        // back-left
                new Vector3(halfW, 0, botZ),         // back-right
                Vector3.up,   // 法线向上（从下方可见）
                segments, segments);
        }

        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(triangles, 0);
        _mesh.SetNormals(normals);
        _mesh.SetUVs(0, uvs);
        _mesh.RecalculateBounds();

        _meshFilter.sharedMesh = _mesh;

        // 缓存参数
        _cachedWidth = width;
        _cachedHeight = height;
        _cachedDepth = depth;
        _cachedTopDepth = topDepth;
        _cachedSegments = segments;
        _cachedBack = enableBack;
        _cachedTop = enableTop;
        _cachedLeft = enableLeft;
        _cachedRight = enableRight;
        _cachedBottom = enableBottom;
    }

    /// <summary>
    /// 在网格中添加一个由 (segX × segY) 个四边形细分的面
    /// </summary>
    private static void AddGridFace(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        Vector3 v00, Vector3 v10, // 第一行: front-left, front-right
        Vector3 v01, Vector3 v11, // 第二行: back-left,  back-right
        Vector3 normal,
        int segX, int segY)
    {
        int vertStart = vertices.Count;

        // 生成顶点网格 (segX+1) × (segY+1)
        for (int y = 0; y <= segY; y++)
        {
            float ty = y / (float)segY;
            Vector3 left = Vector3.Lerp(v00, v01, ty);
            Vector3 right = Vector3.Lerp(v10, v11, ty);

            for (int x = 0; x <= segX; x++)
            {
                float tx = x / (float)segX;
                vertices.Add(Vector3.Lerp(left, right, tx));
                normals.Add(normal);
                uvs.Add(new Vector2(tx, ty));
            }
        }

        // 生成三角形索引
        int stride = segX + 1;
        for (int y = 0; y < segY; y++)
        {
            for (int x = 0; x < segX; x++)
            {
                int i = vertStart + y * stride + x;

                // 两个三角形组成一个四边形
                triangles.Add(i);
                triangles.Add(i + stride);
                triangles.Add(i + 1);

                triangles.Add(i + 1);
                triangles.Add(i + stride);
                triangles.Add(i + stride + 1);
            }
        }
    }

    private void OnDestroy()
    {
        if (_mesh != null)
        {
            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
        }
    }
}
