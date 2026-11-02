// Builds one prefab per layout json in BlockKit/Layouts (Prefabs/<root>.prefab), e.g. Layouts/BlockGym_Layout.json -> Prefabs/BlockGym.prefab.
// Menu: BlockKit > Build Layout Prefabs. Safe to run again: prefabs are overwritten.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BlockKitBuilder
{
    const string PrefKey = "BlockKit.UseFbxSpace";

    [MenuItem("BlockKit/Build Layout Prefabs")]
    public static void BuildAll()
    {
        int n = 0;
        foreach (var g in AssetDatabase.FindAssets("t:TextAsset"))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.Replace('\\', '/').Contains("/BlockKit/Layouts/") && p.EndsWith(".json")) { Build(p); n++; }
        }
        if (n == 0) Debug.LogError("BlockKit: no layout json found in BlockKit/Layouts.");
    }

    [MenuItem("Assets/BlockKit/Build Prefab from this layout")]
    public static void BuildSelected() { foreach (var o in Selection.objects) { var p = AssetDatabase.GetAssetPath(o); if (p.EndsWith(".json")) Build(p); } }
    [MenuItem("Assets/BlockKit/Build Prefab from this layout", true)]
    static bool BuildSelectedValid() => Selection.activeObject && AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".json");

    public static void Build(string layoutPath)
    {
        string kit = layoutPath.Replace('\\', '/');
        kit = kit.Substring(0, kit.IndexOf("/Layouts/"));          // .../BlockKit
        var layout = BlockKitLayout.Parse(File.ReadAllText(layoutPath));
        bool useFbx = EditorPrefs.GetBool(PrefKey, false);
        string rootName = string.IsNullOrEmpty(layout.root) ? Path.GetFileNameWithoutExtension(layoutPath) : layout.root;

        var root = new GameObject(rootName);
        try
        {
            foreach (var pl in layout.placements)
            {
                string modelPath = $"{kit}/Models/{pl.model}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (!model) { Debug.LogWarning("BlockKit: missing model " + modelPath); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                var pose = useFbx ? pl.fbx : pl.unity;
                go.name = pl.name;
                go.transform.localPosition = pose.Position;
                go.transform.localRotation = pose.Rotation;
            }
            if (layout.spawns != null)
                foreach (var sp in layout.spawns)
                {
                    var go = new GameObject(sp.name);
                    go.transform.SetParent(root.transform, false);
                    var pose = useFbx ? sp.fbx : sp.unity;
                    go.transform.localPosition = pose.Position;
                    go.transform.localRotation = pose.Rotation;
                }
            var scene = root.AddComponent<BlockKitScene>();
            scene.FindAssets();

            string prefabDir = kit + "/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabDir)) AssetDatabase.CreateFolder(kit, "Prefabs");
            string prefabPath = $"{prefabDir}/{rootName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool ok);
            Debug.Log(ok ? $"BlockKit: {prefabPath} ({layout.placements.Length} models, {(layout.spawns == null ? 0 : layout.spawns.Length)} spawn points, {(useFbx ? "fbx" : "unity")} space)"
                         : "BlockKit: failed to save " + prefabPath);
            if (ok) Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    // If a layout comes out mirrored against the models (e.g. treadmill consoles facing the wrong way),
    // switch to the raw FBX coordinates and build again.
    [MenuItem("BlockKit/Toggle coordinate space (unity <-> fbx)")]
    public static void Toggle()
    {
        bool v = !EditorPrefs.GetBool(PrefKey, false);
        EditorPrefs.SetBool(PrefKey, v);
        Debug.Log("BlockKit: layout space = " + (v ? "fbx" : "unity") + ". Run BlockKit > Build Layout Prefabs again.");
    }
}
