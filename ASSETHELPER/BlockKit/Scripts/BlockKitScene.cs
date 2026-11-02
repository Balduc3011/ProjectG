// Spawns the BlockBuddy characters on the Spawn_* points under this object and starts their routines.
// BlockKit > Build Layout Prefabs creates the prefab (models + spawn points + this component) from the layout json.
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BlockKitScene : MonoBehaviour
{
    [Tooltip("BlockBuddy model (BlockBuddy/Models/BlockBuddy.fbx)")]
    public GameObject blockBuddy;
    [Tooltip("All BlockBuddy clips (BlockBuddy/Animations/*.fbx)")]
    public List<AnimationClip> clips = new List<AnimationClip>();
    [Tooltip("Skins given to the characters in turn (optional)")]
    public List<Texture2D> skins = new List<Texture2D>();
    public bool spawnOnStart = true;

    readonly List<GameObject> spawned = new List<GameObject>();

    void Start() { if (spawnOnStart) Build(); }

    [ContextMenu("Build characters")]
    public void Build()
    {
        Clear();
        if (!blockBuddy) { Debug.LogError("BlockKitScene: assign the BlockBuddy model."); return; }
        var byName = new Dictionary<string, AnimationClip>();
        foreach (var c in clips) if (c) byName[c.name] = c;

        var spawns = new List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Spawn_")) spawns.Add(t);
        if (spawns.Count == 0) { Debug.LogError("BlockKitScene: no Spawn_* points found. Build the prefab with BlockKit > Build Layout Prefabs."); return; }

        var barbell = BlockKitActor.FindDeep(transform, "Barbell");
        var dbR = BlockKitActor.FindDeep(transform, "Dumbbell_1");
        var dbL = BlockKitActor.FindDeep(transform, "Dumbbell_2");
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("HeavyBag_") && !t.GetComponent<BlockKitBag>()) t.gameObject.AddComponent<BlockKitBag>();

        int i = 0;
        foreach (var sp in spawns)
        {
            // Spawn_<Role>_<n>[@<target>]
            string spec = sp.name.Substring(6), target = null;
            int at = spec.IndexOf('@');
            if (at >= 0) { target = spec.Substring(at + 1); spec = spec.Substring(0, at); }
            string roleName = spec.Split('_')[0];
            if (!System.Enum.TryParse(roleName, out BlockKitActor.Role role)) { Debug.LogWarning("BlockKitScene: unknown role " + sp.name); continue; }

            var go = Instantiate(blockBuddy, sp.position, sp.rotation, transform);
            go.name = "BlockBuddy_" + spec;
            spawned.Add(go);
            ApplySkin(go, i);
            var actor = go.AddComponent<BlockKitActor>();
            if (role == BlockKitActor.Role.BenchPress) actor.barbell = barbell;
            if (role == BlockKitActor.Role.DumbbellCurl) { actor.dumbbellRight = dbR; actor.dumbbellLeft = dbL; }
            if (role == BlockKitActor.Role.BagBoxer && target != null) { var b = BlockKitActor.FindDeep(transform, target); if (b) actor.bag = b.GetComponent<BlockKitBag>(); }
            actor.Init(role, byName, i * 1.7f);
            i++;
        }
    }

    public void Clear()
    {
        foreach (var go in spawned) if (go) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
        spawned.Clear();
    }

    void ApplySkin(GameObject go, int index)
    {
        if (skins.Count == 0) return;
        var tex = skins[index % skins.Count];
        if (!tex) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var m = new Material(r.sharedMaterial); m.mainTexture = tex; r.sharedMaterial = m;
        }
    }

#if UNITY_EDITOR
    // Fill the asset references automatically from the BlockBuddy folder.
    void OnValidate() { if (!blockBuddy || clips.Count == 0) FindAssets(); }
    void Awake() { if (!blockBuddy || clips.Count == 0) FindAssets(); }

    [ContextMenu("Find BlockBuddy assets")]
    public void FindAssets()
    {
        if (!blockBuddy)
            foreach (var g in AssetDatabase.FindAssets("BlockBuddy t:Model"))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (path.EndsWith("/Models/BlockBuddy.fbx")) { blockBuddy = AssetDatabase.LoadAssetAtPath<GameObject>(path); break; }
            }
        if (clips.Count == 0)
            foreach (var g in AssetDatabase.FindAssets("BlockBuddy@ t:Model"))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.Contains("/BlockBuddy/Animations/")) continue;
                foreach (var a in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                    if (a is AnimationClip c && !c.name.StartsWith("__preview__")) clips.Add(c);
            }
        if (skins.Count == 0)
            foreach (var g in AssetDatabase.FindAssets("Skin_ t:Texture2D"))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (path.Contains("/BlockBuddy/Textures/")) skins.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            }
    }
#endif
}
