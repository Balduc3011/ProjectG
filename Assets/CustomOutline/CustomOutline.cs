using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Runtime outline: full triangle edges (barycentric wireframe) or outer shell silhouette.
/// Works with MeshFilter or SkinnedMeshRenderer; child renderer stays on when the main renderer is off.
/// Pick URP vs Built-in shader from GraphicsSettings.defaultRenderPipeline.
/// </summary>
[DisallowMultipleComponent]
public class CustomOutline : MonoBehaviour
{
    private const string UrpShaderName = "CustomOutline/URP";
    private const string BuiltInShaderName = "CustomOutline/BuiltIn";
    private const string ChildName = "__CustomOutlineRenderer";
    private const string UrpAssetTypeName = "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset";

    public Color outlineColor = new Color(0f, 1f, 0.5f, 1f);
    public float outlineWidth = 1f;
    public float outlineEmission = 1f;

    /// <summary>
    /// True: all triangle edges (wireframe). False: inflated hull + front-face cull (typical outer outline).
    /// </summary>
    public bool allEdge = true;

    private MeshFilter _meshFilter;
    private SkinnedMeshRenderer _skinnedMeshRenderer;
    private Mesh _sourceMesh;
    private bool _useSkinnedMesh;

    private Mesh _wireMesh;
    private Mesh _silhouetteMesh;
    private Mesh _bakeMesh;
    private readonly List<int> _wireSourceVertexIndices = new List<int>(1024);

    private List<Vector3> _scratchWirePositions;
    private List<Vector3> _scratchSilhouettePositions;
    private List<Vector3> _scratchSilhouetteNormals;

    private GameObject _outlineChild;
    private MeshFilter _childMeshFilter;
    private MeshRenderer _outlineRenderer;
    private Material _outlineMaterial;

    private bool _isBuilt;
    private bool _isActive;
    private bool _lastAllEdge = true;
    private Color _lastAppliedColor;
    private float _lastAppliedWidth;
    private float _lastAppliedEmission;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnDestroy()
    {
        if (_wireMesh != null)
        {
            Destroy(_wireMesh);
            _wireMesh = null;
        }

        if (_silhouetteMesh != null)
        {
            Destroy(_silhouetteMesh);
            _silhouetteMesh = null;
        }

        if (_bakeMesh != null)
        {
            Destroy(_bakeMesh);
            _bakeMesh = null;
        }

        if (_outlineMaterial != null)
        {
            Destroy(_outlineMaterial);
            _outlineMaterial = null;
        }

        if (_outlineChild != null)
        {
            Destroy(_outlineChild);
            _outlineChild = null;
        }
    }

    private void CacheComponents()
    {
        _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();

        if (_skinnedMeshRenderer != null && _skinnedMeshRenderer.sharedMesh != null)
        {
            _useSkinnedMesh = true;
            _sourceMesh = _skinnedMeshRenderer.sharedMesh;
            return;
        }

        _useSkinnedMesh = false;
        _sourceMesh = _meshFilter != null ? _meshFilter.sharedMesh : null;
    }

    private void LateUpdate()
    {
        if (!_isBuilt || !_isActive || _outlineRenderer == null || !_outlineRenderer.enabled)
        {
            return;
        }

        if (_lastAllEdge != allEdge)
        {
            _lastAllEdge = allEdge;
            ApplyVisualMode();
        }

        if (_useSkinnedMesh && _skinnedMeshRenderer != null)
        {
            UpdateSkinnedMeshGeometry();
        }

        if (ColorChanged(outlineColor, _lastAppliedColor) ||
            !Mathf.Approximately(outlineWidth, _lastAppliedWidth) ||
            !Mathf.Approximately(outlineEmission, _lastAppliedEmission))
        {
            PushMaterialProperties();
        }

        if (_outlineChild != null && _outlineChild.layer != gameObject.layer)
        {
            _outlineChild.layer = gameObject.layer;
        }
    }

    /// <summary>Enable outline rendering.</summary>
    [Button]
    public void Active()
    {
        CacheComponents();
        EnsureBuilt();
        if (_outlineRenderer == null)
        {
            return;
        }

        _isActive = true;
        _outlineRenderer.enabled = true;
        ApplyVisualMode();
        PushMaterialProperties();

        if (_useSkinnedMesh && _skinnedMeshRenderer != null)
        {
            UpdateSkinnedMeshGeometry();
        }
    }

    /// <summary>Disable outline rendering.</summary>
    [Button]
    public void Deactive()
    {
        _isActive = false;
        if (_outlineRenderer != null)
        {
            _outlineRenderer.enabled = false;
        }
    }

    private static bool IsUniversalRpActive()
    {
        RenderPipelineAsset rp = GraphicsSettings.defaultRenderPipeline;
        if (rp == null)
        {
            return false;
        }

        Type t = rp.GetType();
        return t.FullName == UrpAssetTypeName || (t.FullName != null && t.FullName.IndexOf("UniversalRenderPipeline", StringComparison.Ordinal) >= 0);
    }

    private static string ResolveShaderResourceName()
    {
        return IsUniversalRpActive() ? UrpShaderName : BuiltInShaderName;
    }

    private void EnsureBuilt()
    {
        if (_isBuilt)
        {
            return;
        }

        CacheComponents();
        if (_sourceMesh == null)
        {
            return;
        }

        RenderPipelineAsset activeRp = GraphicsSettings.defaultRenderPipeline;
        if (activeRp != null && !IsUniversalRpActive())
        {
            Debug.LogWarning(
                "CustomOutline: Active render pipeline is not Universal RP; using the Built-in outline shader. For URP projects this is unexpected.",
                this);
        }

        string shaderName = ResolveShaderResourceName();
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError(
                "CustomOutline: Shader '" + shaderName +
                "' not found. Add both URP and Built-in shader assets under Assets/CustomOutline and reference one in the scene or Always Included Shaders.",
                this);
            return;
        }

        _wireSourceVertexIndices.Clear();
        _wireMesh = BuildBarycentricExpandedMesh(_sourceMesh, _wireSourceVertexIndices);
        if (_wireMesh == null)
        {
            return;
        }

        _silhouetteMesh = DuplicateMeshForOutline(_sourceMesh);
        if (_silhouetteMesh == null)
        {
            Destroy(_wireMesh);
            _wireMesh = null;
            return;
        }

        int wireCount = _wireSourceVertexIndices.Count;
        _scratchWirePositions = new List<Vector3>(wireCount);
        for (int i = 0; i < wireCount; i++)
        {
            _scratchWirePositions.Add(Vector3.zero);
        }

        int silVc = _silhouetteMesh.vertexCount;
        _scratchSilhouettePositions = new List<Vector3>(silVc);
        _scratchSilhouetteNormals = new List<Vector3>(silVc);
        for (int i = 0; i < silVc; i++)
        {
            _scratchSilhouettePositions.Add(Vector3.zero);
            _scratchSilhouetteNormals.Add(Vector3.zero);
        }

        if (_useSkinnedMesh)
        {
            _bakeMesh = new Mesh();
            _bakeMesh.name = _sourceMesh.name + "_BakeScratch";
            _bakeMesh.MarkDynamic();
        }

        if (_outlineMaterial == null)
        {
            _outlineMaterial = new Material(shader);
        }

        if (_outlineChild == null)
        {
            _outlineChild = new GameObject(ChildName);
            _outlineChild.transform.SetParent(transform, false);
            _outlineChild.transform.localPosition = Vector3.zero;
            _outlineChild.transform.localRotation = Quaternion.identity;
            _outlineChild.transform.localScale = Vector3.one;
            _outlineChild.layer = gameObject.layer;

            _childMeshFilter = _outlineChild.AddComponent<MeshFilter>();
            _childMeshFilter.sharedMesh = _wireMesh;

            _outlineRenderer = _outlineChild.AddComponent<MeshRenderer>();
            _outlineRenderer.sharedMaterial = _outlineMaterial;
            _outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _outlineRenderer.receiveShadows = false;
            _outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
            _outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _outlineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            _outlineRenderer.enabled = false;

            Renderer sourceRenderer = GetComponent<Renderer>();
            if (sourceRenderer != null)
            {
                _outlineRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
                _outlineRenderer.sortingOrder = sourceRenderer.sortingOrder;
            }

            CopyMaterialsSlotCount(_sourceMesh, _outlineRenderer);
        }

        _lastAllEdge = allEdge;
        _isBuilt = true;
    }

    private void ApplyVisualMode()
    {
        if (_childMeshFilter == null || _outlineMaterial == null)
        {
            return;
        }

        _childMeshFilter.sharedMesh = allEdge ? _wireMesh : _silhouetteMesh;

        if (allEdge)
        {
            _outlineMaterial.DisableKeyword("CUSTOMOUTLINE_SILHOUETTE");
            _outlineMaterial.SetFloat("_OutlineCull", (float)CullMode.Off);
        }
        else
        {
            _outlineMaterial.EnableKeyword("CUSTOMOUTLINE_SILHOUETTE");
            _outlineMaterial.SetFloat("_OutlineCull", (float)CullMode.Front);
        }
    }

    private void UpdateSkinnedMeshGeometry()
    {
        if (_skinnedMeshRenderer == null || _bakeMesh == null || _wireMesh == null || _silhouetteMesh == null)
        {
            return;
        }

        _skinnedMeshRenderer.BakeMesh(_bakeMesh);
        Vector3[] bakedVerts = _bakeMesh.vertices;
        if (bakedVerts == null || bakedVerts.Length == 0)
        {
            return;
        }

        int bakedVc = bakedVerts.Length;
        if (bakedVc != _scratchSilhouettePositions.Count)
        {
            return;
        }

        for (int i = 0; i < bakedVc; i++)
        {
            _scratchSilhouettePositions[i] = bakedVerts[i];
        }

        _silhouetteMesh.SetVertices(_scratchSilhouettePositions);

        Vector3[] bakedNrms = _bakeMesh.normals;
        if (bakedNrms != null && bakedNrms.Length == bakedVc)
        {
            for (int i = 0; i < bakedVc; i++)
            {
                _scratchSilhouetteNormals[i] = bakedNrms[i];
            }

            _silhouetteMesh.SetNormals(_scratchSilhouetteNormals);
        }

        _silhouetteMesh.RecalculateBounds();

        int wireCount = _wireSourceVertexIndices.Count;
        for (int i = 0; i < wireCount; i++)
        {
            int src = _wireSourceVertexIndices[i];
            if ((uint)src < (uint)bakedVerts.Length)
            {
                _scratchWirePositions[i] = bakedVerts[src];
            }
        }

        _wireMesh.SetVertices(_scratchWirePositions);
        _wireMesh.RecalculateBounds();
    }

    private static void CopyMaterialsSlotCount(Mesh source, MeshRenderer targetRenderer)
    {
        int count = source.subMeshCount;
        if (count <= 0)
        {
            count = 1;
        }

        Material mat = targetRenderer.sharedMaterial;
        if (mat == null)
        {
            return;
        }

        if (count == 1)
        {
            targetRenderer.sharedMaterial = mat;
            return;
        }

        Material[] mats = new Material[count];
        for (int i = 0; i < count; i++)
        {
            mats[i] = mat;
        }

        targetRenderer.sharedMaterials = mats;
    }

    private void PushMaterialProperties()
    {
        if (_outlineMaterial == null)
        {
            return;
        }

        // Unique Material per instance: set on material (safe before Awake / Odin editor buttons).
        // Clear renderer MPB so no stale overrides hide color or other props.
        _outlineMaterial.SetColor("_OutlineColor", outlineColor);
        _outlineMaterial.SetFloat("_OutlineWidth", Mathf.Max(outlineWidth, 1e-4f));
        _outlineMaterial.SetFloat("_OutlineEmission", Mathf.Max(outlineEmission, 0f));
        if (_outlineRenderer != null)
        {
            _outlineRenderer.SetPropertyBlock(null);
        }

        _lastAppliedColor = outlineColor;
        _lastAppliedWidth = outlineWidth;
        _lastAppliedEmission = outlineEmission;
    }

    private static bool ColorChanged(Color a, Color b)
    {
        return !Mathf.Approximately(a.r, b.r) ||
               !Mathf.Approximately(a.g, b.g) ||
               !Mathf.Approximately(a.b, b.b) ||
               !Mathf.Approximately(a.a, b.a);
    }

    private static Mesh DuplicateMeshForOutline(Mesh source)
    {
        if (source == null)
        {
            return null;
        }

        Mesh m = new Mesh();
        m.name = source.name + "_OutlineHull";
        m.vertices = source.vertices;
        if (source.normals != null && source.normals.Length == source.vertexCount)
        {
            m.normals = source.normals;
        }

        m.subMeshCount = source.subMeshCount;
        for (int s = 0; s < source.subMeshCount; s++)
        {
            m.SetTriangles(source.GetTriangles(s), s);
        }

        if (m.normals == null || m.normals.Length != m.vertexCount)
        {
            m.RecalculateNormals();
        }

        int vc = m.vertexCount;
        List<Vector3> zeroBary = new List<Vector3>(vc);
        for (int i = 0; i < vc; i++)
        {
            zeroBary.Add(Vector3.zero);
        }

        m.SetUVs(1, zeroBary);
        m.RecalculateBounds();
        return m;
    }

    private static Mesh BuildBarycentricExpandedMesh(Mesh source, List<int> sourceVertexRemap)
    {
        if (source == null)
        {
            return null;
        }

        int subMeshCount = source.subMeshCount;
        if (subMeshCount <= 0)
        {
            return null;
        }

        sourceVertexRemap.Clear();

        int triangleCount = 0;
        for (int s = 0; s < subMeshCount; s++)
        {
            triangleCount += source.GetTriangles(s).Length / 3;
        }

        int vertexCapacity = triangleCount * 3;
        List<Vector3> positions = new List<Vector3>(vertexCapacity);
        List<Vector3> normals = new List<Vector3>(vertexCapacity);
        List<Vector3> baryUV1 = new List<Vector3>(vertexCapacity);

        bool hasNormals = source.normals != null && source.normals.Length == source.vertexCount;

        for (int s = 0; s < subMeshCount; s++)
        {
            int[] tris = source.GetTriangles(s);
            int triLen = tris.Length;
            for (int t = 0; t < triLen; t += 3)
            {
                int i0 = tris[t];
                int i1 = tris[t + 1];
                int i2 = tris[t + 2];

                AppendCorner(source, positions, normals, baryUV1, sourceVertexRemap, hasNormals, i0, new Vector3(1f, 0f, 0f));
                AppendCorner(source, positions, normals, baryUV1, sourceVertexRemap, hasNormals, i1, new Vector3(0f, 1f, 0f));
                AppendCorner(source, positions, normals, baryUV1, sourceVertexRemap, hasNormals, i2, new Vector3(0f, 0f, 1f));
            }
        }

        Mesh wire = new Mesh();
        wire.name = source.name + "_WireframeBarycentric";
        wire.indexFormat = vertexCapacity > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        wire.SetVertices(positions);
        if (hasNormals)
        {
            wire.SetNormals(normals);
        }

        wire.SetUVs(1, baryUV1);
        wire.subMeshCount = subMeshCount;

        int v = 0;
        for (int s = 0; s < subMeshCount; s++)
        {
            int[] tris = source.GetTriangles(s);
            int triLen = tris.Length;
            int[] newTris = new int[triLen];
            for (int t = 0; t < triLen; t += 3)
            {
                newTris[t] = v;
                newTris[t + 1] = v + 1;
                newTris[t + 2] = v + 2;
                v += 3;
            }

            wire.SetTriangles(newTris, s);
        }

        if (!hasNormals)
        {
            wire.RecalculateNormals();
        }

        wire.RecalculateBounds();
        return wire;
    }

    private static void AppendCorner(
        Mesh source,
        List<Vector3> positions,
        List<Vector3> normals,
        List<Vector3> baryUV1,
        List<int> sourceVertexRemap,
        bool hasNormals,
        int vertexIndex,
        Vector3 barycentric)
    {
        positions.Add(source.vertices[vertexIndex]);
        sourceVertexRemap.Add(vertexIndex);
        if (hasNormals)
        {
            normals.Add(source.normals[vertexIndex]);
        }

        baryUV1.Add(barycentric);
    }
}
