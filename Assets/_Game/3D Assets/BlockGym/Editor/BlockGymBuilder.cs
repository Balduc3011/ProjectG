// Builds Prefabs/BlockGym.prefab from BlockGym_Layout.json and the model FBX files.
// Menu: BlockGym > Build Scene Prefab. Safe to run again: the prefab is overwritten.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BlockGymBuilder
{
    [MenuItem("BlockGym/Build Scene Prefab")]
    public static void Build()
    {
        string layoutPath = FindLayout();
        if (layoutPath == null) { Debug.LogError("BlockGym: BlockGym_Layout.json not found."); return; }
        string folder = Path.GetDirectoryName(layoutPath).Replace('\\', '/');
        var layout = BlockGymLayout.Parse(File.ReadAllText(layoutPath));
        bool useFbx = EditorPrefs.GetBool("BlockGym.UseFbxSpace", false);   // see BlockGym > Toggle coordinate space

        var root = new GameObject(string.IsNullOrEmpty(layout.root) ? "BlockGym" : layout.root);
        try
        {
            foreach (var pl in layout.placements)
            {
                string modelPath = $"{folder}/Models/{pl.model}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (!model) { Debug.LogWarning("BlockGym: missing model " + modelPath); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                var pose = useFbx ? pl.fbx : pl.unity;
                go.name = pl.name;
                go.transform.localPosition = pose.Position;
                go.transform.localRotation = pose.Rotation;
            }
            foreach (var sp in layout.spawns)
            {
                var go = new GameObject(sp.name);
                go.transform.SetParent(root.transform, false);
                var pose = useFbx ? sp.fbx : sp.unity;
                go.transform.localPosition = pose.Position;
                go.transform.localRotation = pose.Rotation;
            }
            var scene = root.AddComponent<BlockGymScene>();
            scene.FindAssets();

            string prefabDir = folder + "/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabDir)) AssetDatabase.CreateFolder(folder, "Prefabs");
            string prefabPath = prefabDir + "/BlockGym.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool ok);
            Debug.Log(ok ? $"BlockGym: prefab written to {prefabPath} ({layout.placements.Length} models, {layout.spawns.Length} spawn points, {(useFbx ? "fbx" : "unity")} space)"
                         : "BlockGym: failed to save prefab");
            if (ok) Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    // If the room comes out mirrored against the models (e.g. treadmill consoles facing the wrong way),
    // switch to the raw FBX coordinates and build again.
    [MenuItem("BlockGym/Toggle coordinate space (unity <-> fbx)")]
    public static void Toggle()
    {
        bool v = !EditorPrefs.GetBool("BlockGym.UseFbxSpace", false);
        EditorPrefs.SetBool("BlockGym.UseFbxSpace", v);
        Debug.Log("BlockGym: layout space = " + (v ? "fbx" : "unity") + ". Run BlockGym > Build Scene Prefab again.");
    }

    static string FindLayout()
    {
        foreach (var g in AssetDatabase.FindAssets("BlockGym_Layout t:TextAsset"))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.EndsWith("/BlockGym/BlockGym_Layout.json")) return p;   // Models/ is resolved next to it
        }
        return null;
    }
}
