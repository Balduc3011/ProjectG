//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]

public class Outline : MonoBehaviour {
  private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

  public enum Mode {
    OutlineAll,
    OutlineVisible,
    OutlineHidden,
    OutlineAndSilhouette,
    SilhouetteOnly,
    /// <summary>Wireframe: only triangle edges (MeshFilter + Read/Write mesh).</summary>
    WireframeEdgesOnly,
    /// <summary>Wireframe: shaded triangle interior + edge lines.</summary>
    WireframeSurfaceAndEdges,
    /// <summary>Wireframe: shaded triangle interior without edge lines.</summary>
    WireframeSurfaceNoEdges
  }

  public static bool IsTopologyWireframeMode(Mode mode) {
    return mode == Mode.WireframeEdgesOnly || mode == Mode.WireframeSurfaceAndEdges || mode == Mode.WireframeSurfaceNoEdges;
  }

  const float WireframeDrawModeOff = 0f;
  const float WireframeDrawModeEdgesOnly = 1f;
  const float WireframeDrawModeSurfaceEdges = 2f;
  const float WireframeDrawModeSurfaceNoEdges = 3f;

  public Mode OutlineMode {
    get { return outlineMode; }
    set {
      outlineMode = value;
      needsUpdate = true;
    }
  }

  public Color OutlineColor {
    get { return outlineColor; }
    set {
      outlineColor = value;
      needsUpdate = true;
    }
  }

  public float OutlineWidth {
    get { return outlineWidth; }
    set {
      outlineWidth = value;
      needsUpdate = true;
    }
  }

  public float OutlineEmission {
    get { return outlineEmission; }
    set {
      outlineEmission = Mathf.Clamp(value, 0f, 10f);
      needsUpdate = true;
    }
  }

  [Serializable]
  private class ListVector3 {
    public List<Vector3> data;
  }

  [SerializeField]
  private Mode outlineMode;

  [SerializeField]
  private Color outlineColor = Color.white;

  [SerializeField, Range(0f, 10f)]
  private float outlineWidth = 2f;

  [SerializeField, Range(0f, 10f)]
  private float outlineEmission = 1f;

  [SerializeField, ReadOnly, Tooltip("Total vertex count of MeshFilter + SkinnedMeshRenderer meshes under this object. Cached once when the component is added (Reset) or on first Awake if not yet cached.")]
  private int _meshVertexCount;

  [SerializeField, HideInInspector]
  private bool _meshVertexCountCached;

  [Header("Optional")]

  [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
  + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
  private bool precomputeOutline;

  [SerializeField, HideInInspector]
  private List<Mesh> bakeKeys = new List<Mesh>();

  [SerializeField, HideInInspector]
  private List<ListVector3> bakeValues = new List<ListVector3>();

  private Renderer[] renderers;
  private Material outlineMaskMaterial;
  private Material outlineFillMaterial;

  private bool needsUpdate;

  private readonly HashSet<Mesh> _wireframeTopologyMeshes = new HashSet<Mesh>();
  private readonly List<Mesh> _createdRuntimeWireframeMeshes = new List<Mesh>();

  static bool UseUrpOutlineResources() {
    var rp = GraphicsSettings.defaultRenderPipeline;
    if (rp == null) {
      return false;
    }

    var typeName = rp.GetType().Name;
    return typeName.IndexOf("Universal", StringComparison.OrdinalIgnoreCase) >= 0;
  }

  void Reset() {
    _meshVertexCountCached = false;
    CacheMeshVertexCount();
  }

  void CacheMeshVertexCount() {
    if (_meshVertexCountCached) {
      return;
    }

    int total = 0;
    var meshFilters = GetComponentsInChildren<MeshFilter>(true);
    for (int i = 0; i < meshFilters.Length; i++) {
      var meshFilter = meshFilters[i];
      if (meshFilter == null || meshFilter.sharedMesh == null) {
        continue;
      }

      total += meshFilter.sharedMesh.vertexCount;
    }

    var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
    for (int i = 0; i < skinnedRenderers.Length; i++) {
      var skinnedMeshRenderer = skinnedRenderers[i];
      if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null) {
        continue;
      }

      total += skinnedMeshRenderer.sharedMesh.vertexCount;
    }

    _meshVertexCount = total;
    _meshVertexCountCached = true;
  }

  void Awake() {
    CacheMeshVertexCount();

    // Cache renderers
    renderers = GetComponentsInChildren<Renderer>();

    // Instantiate outline materials (Built-in vs URP)
    var useUrp = UseUrpOutlineResources();
    var maskResource = useUrp ? @"Materials/OutlineMaskURP" : @"Materials/OutlineMask";
    var fillResource = useUrp ? @"Materials/OutlineFillURP" : @"Materials/OutlineFill";

    var maskSource = Resources.Load<Material>(maskResource);
    var fillSource = Resources.Load<Material>(fillResource);
    outlineMaskMaterial = Instantiate(maskSource);
    outlineFillMaterial = Instantiate(fillSource);

    outlineMaskMaterial.name = "OutlineMask (Instance)";
    outlineFillMaterial.name = "OutlineFill (Instance)";

    if (IsTopologyWireframeMode(outlineMode)) {
      PrepareWireframeExplodedMeshes();
    }

    // Retrieve or generate smooth normals
    LoadSmoothNormals();

    // Sync GPU state before first Update (stencil / wireframe must match mode immediately)
    UpdateMaterialProperties();
    needsUpdate = true;
  }

  void OnEnable() {
    if (renderers == null || outlineMaskMaterial == null || outlineFillMaterial == null) {
      return;
    }

    foreach (var renderer in renderers) {

      // Append outline shaders
      var materials = renderer.sharedMaterials.ToList();

      materials.Add(outlineMaskMaterial);
      materials.Add(outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }

  void OnValidate() {

    // Update material properties
    needsUpdate = true;

    // Clear cache when baking is disabled or corrupted
    if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count) {
      bakeKeys.Clear();
      bakeValues.Clear();
    }

    // Generate smooth normals when baking is enabled
    if (precomputeOutline && bakeKeys.Count == 0) {
      Bake();
    }
  }

  void Update() {
    if (!needsUpdate || outlineFillMaterial == null || outlineMaskMaterial == null) {
      return;
    }

    needsUpdate = false;

    UpdateMaterialProperties();
  }

  void OnDisable() {
    if (renderers == null || outlineMaskMaterial == null || outlineFillMaterial == null) {
      return;
    }

    foreach (var renderer in renderers) {

      // Remove outline shaders
      var materials = renderer.sharedMaterials.ToList();

      materials.Remove(outlineMaskMaterial);
      materials.Remove(outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }

  void OnDestroy() {

    // Destroy material instances
    if (outlineMaskMaterial != null) {
      Destroy(outlineMaskMaterial);
    }

    if (outlineFillMaterial != null) {
      Destroy(outlineFillMaterial);
    }

    _createdRuntimeWireframeMeshes.Clear();
    _wireframeTopologyMeshes.Clear();
  }

  void Bake() {

    // Generate smooth normals for each mesh
    var bakedMeshes = new HashSet<Mesh>();

    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {

      // Skip duplicates
      if (!bakedMeshes.Add(meshFilter.sharedMesh)) {
        continue;
      }

      // Serialize smooth normals
      var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

      bakeKeys.Add(meshFilter.sharedMesh);
      bakeValues.Add(new ListVector3() { data = smoothNormals });
    }
  }

  void LoadSmoothNormals() {

    // Retrieve or generate smooth normals
    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {

      var mesh = meshFilter.sharedMesh;
      if (mesh == null) {
        continue;
      }

      if (_wireframeTopologyMeshes.Contains(mesh)) {

        if (!registeredMeshes.Add(mesh)) {
          continue;
        }

        var normals = mesh.normals;
        var flatNormals = new List<Vector3>(normals.Length);
        for (int vi = 0; vi < normals.Length; vi++) {
          flatNormals.Add(normals[vi]);
        }

        mesh.SetUVs(3, flatNormals);

        var renderer = meshFilter.GetComponent<Renderer>();
        if (renderer != null) {
          CombineSubmeshes(mesh, renderer.sharedMaterials);
        }

        continue;
      }

      // Skip if smooth normals have already been adopted
      if (!registeredMeshes.Add(mesh)) {
        continue;
      }

      // Retrieve or generate smooth normals
      var index = bakeKeys.IndexOf(mesh);
      var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(mesh);

      // Store smooth normals in UV3
      mesh.SetUVs(3, smoothNormals);

      // Combine submeshes
      var renderer2 = meshFilter.GetComponent<Renderer>();

      if (renderer2 != null) {
        CombineSubmeshes(mesh, renderer2.sharedMaterials);
      }
    }

    // Clear UV3 on skinned mesh renderers
    foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) {

      // Skip if UV3 has already been reset
      if (!registeredMeshes.Add(skinnedMeshRenderer.sharedMesh)) {
        continue;
      }

      // Clear UV3
      skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];

      // Combine submeshes
      CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
    }
  }

  List<Vector3> SmoothNormals(Mesh mesh) {

    // Group vertices by location
    var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);

    // Copy normals to a new list
    var smoothNormals = new List<Vector3>(mesh.normals);

    // Average normals for grouped vertices
    foreach (var group in groups) {

      // Skip single vertices
      if (group.Count() == 1) {
        continue;
      }

      // Calculate the average normal
      var smoothNormal = Vector3.zero;

      foreach (var pair in group) {
        smoothNormal += smoothNormals[pair.Value];
      }

      smoothNormal.Normalize();

      // Assign smooth normal to each vertex
      foreach (var pair in group) {
        smoothNormals[pair.Value] = smoothNormal;
      }
    }

    return smoothNormals;
  }

  void CombineSubmeshes(Mesh mesh, Material[] materials) {

    // Skip meshes with a single submesh
    if (mesh.subMeshCount == 1) {
      return;
    }

    // Skip if submesh count exceeds material count
    if (mesh.subMeshCount > materials.Length) {
      return;
    }

    // Append combined submesh
    mesh.subMeshCount++;
    mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
  }

  void UpdateMaterialProperties() {
    if (outlineFillMaterial == null || outlineMaskMaterial == null) {
      return;
    }

    // Apply properties according to mode
    outlineFillMaterial.SetColor("_OutlineColor", outlineColor);
    outlineFillMaterial.SetFloat("_OutlineEmission", outlineEmission);

    switch (outlineMode) {
      case Mode.OutlineAll:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineFillMaterial.SetFloat("_WireframeDrawMode", WireframeDrawModeOff);
        outlineFillMaterial.SetFloat("_OutlineStencilComp", (float)CompareFunction.NotEqual);
        break;

      case Mode.OutlineVisible:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineFillMaterial.SetFloat("_WireframeDrawMode", WireframeDrawModeOff);
        outlineFillMaterial.SetFloat("_OutlineStencilComp", (float)CompareFunction.NotEqual);
        break;

      case Mode.OutlineHidden:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineFillMaterial.SetFloat("_WireframeDrawMode", WireframeDrawModeOff);
        outlineFillMaterial.SetFloat("_OutlineStencilComp", (float)CompareFunction.NotEqual);
        break;

      case Mode.OutlineAndSilhouette:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineFillMaterial.SetFloat("_WireframeDrawMode", WireframeDrawModeOff);
        outlineFillMaterial.SetFloat("_OutlineStencilComp", (float)CompareFunction.NotEqual);
        break;

      case Mode.SilhouetteOnly:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        outlineFillMaterial.SetFloat("_OutlineWidth", 0f);
        outlineFillMaterial.SetFloat("_WireframeDrawMode", WireframeDrawModeOff);
        outlineFillMaterial.SetFloat("_OutlineStencilComp", (float)CompareFunction.NotEqual);
        break;

      case Mode.WireframeEdgesOnly:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineFillMaterial.SetFloat("_WireframeDrawMode", WireframeDrawModeEdgesOnly);
        outlineFillMaterial.SetFloat("_OutlineStencilComp", (float)CompareFunction.Always);
        break;

      case Mode.WireframeSurfaceAndEdges:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineFillMaterial.SetFloat("_WireframeDrawMode", WireframeDrawModeSurfaceEdges);
        outlineFillMaterial.SetFloat("_OutlineStencilComp", (float)CompareFunction.Always);
        break;

      case Mode.WireframeSurfaceNoEdges:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineFillMaterial.SetFloat("_WireframeDrawMode", WireframeDrawModeSurfaceNoEdges);
        outlineFillMaterial.SetFloat("_OutlineStencilComp", (float)CompareFunction.Always);
        break;
    }
  }

  void PrepareWireframeExplodedMeshes() {
    _wireframeTopologyMeshes.Clear();
    _createdRuntimeWireframeMeshes.Clear();

    var meshFilters = GetComponentsInChildren<MeshFilter>(true);
    for (int i = 0; i < meshFilters.Length; i++) {
      var meshFilter = meshFilters[i];
      if (meshFilter == null) {
        continue;
      }

      var src = meshFilter.sharedMesh;
      if (src == null) {
        continue;
      }

      var clone = Instantiate(src);
      clone.name = src.name + " (OutlineTopology)";
      if (!TryExplodeMeshForBarycentric(clone)) {
        Destroy(clone);
        continue;
      }

      meshFilter.mesh = clone;
      _createdRuntimeWireframeMeshes.Add(clone);
      _wireframeTopologyMeshes.Add(clone);
    }
  }

  static bool TryExplodeMeshForBarycentric(Mesh mesh) {
    if (!mesh.isReadable) {
      Debug.LogWarning("[Outline] Topology wireframe modes need a Read/Write enabled mesh on the Mesh import settings. Skipping explode for '" + mesh.name + "'.");
      return false;
    }

    int subCount = mesh.subMeshCount;
    if (subCount < 1) {
      return false;
    }

    Vector3[] srcVerts = mesh.vertices;
    if (srcVerts == null || srcVerts.Length == 0) {
      return false;
    }

    var srcUv0 = new List<Vector2>();
    mesh.GetUVs(0, srcUv0);
    var hasUv0 = srcUv0.Count == mesh.vertexCount;

    var verts = new List<Vector3>();
    var newUv0 = new List<Vector2>();
    var bary = new List<Vector4>();
    var perSubTris = new List<int>[subCount];

    for (int s = 0; s < subCount; s++) {
      int[] tris = mesh.GetTriangles(s);
      var newTris = new List<int>(tris.Length);
      for (int t = 0; t < tris.Length; t += 3) {
        int ia = tris[t];
        int ib = tris[t + 1];
        int ic = tris[t + 2];
        if (ia < 0 || ib < 0 || ic < 0 || ia >= srcVerts.Length || ib >= srcVerts.Length || ic >= srcVerts.Length) {
          continue;
        }

        int b0 = verts.Count;
        verts.Add(srcVerts[ia]);
        verts.Add(srcVerts[ib]);
        verts.Add(srcVerts[ic]);
        if (hasUv0) {
          newUv0.Add(srcUv0[ia]);
          newUv0.Add(srcUv0[ib]);
          newUv0.Add(srcUv0[ic]);
        }

        bary.Add(new Vector4(1f, 0f, 0f, 0f));
        bary.Add(new Vector4(0f, 1f, 0f, 0f));
        bary.Add(new Vector4(0f, 0f, 1f, 0f));
        newTris.Add(b0);
        newTris.Add(b0 + 1);
        newTris.Add(b0 + 2);
      }

      perSubTris[s] = newTris;
    }

    if (verts.Count == 0) {
      return false;
    }

    mesh.Clear();
    if (verts.Count > 65535) {
      mesh.indexFormat = IndexFormat.UInt32;
    }

    mesh.SetVertices(verts);
    mesh.subMeshCount = subCount;
    for (int s = 0; s < subCount; s++) {
      var triList = perSubTris[s];
      if (triList != null && triList.Count > 0) {
        mesh.SetTriangles(triList, s);
      }
    }

    if (hasUv0 && newUv0.Count == verts.Count) {
      mesh.SetUVs(0, newUv0);
    }

    mesh.RecalculateNormals();
    mesh.RecalculateTangents();
    mesh.SetUVs(4, bary);
    mesh.RecalculateBounds();
    return true;
  }

  [Button("Active")]
  public void Active() {
    enabled = true;
  }

  [Button("Deactive")]
  public void Deactive() {
    enabled = false;
  }
}
