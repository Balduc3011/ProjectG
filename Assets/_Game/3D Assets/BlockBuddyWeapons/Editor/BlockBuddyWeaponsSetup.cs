// BlockBuddyWeapons > Create Weapon Data: Data/WeaponData_<Type>.asset from Data/BlockBuddyWeapons_Clips.json (overwrites in place).
// BlockBuddyWeapons > Build Demo Scene: 4 fighters (Sword / Bow / Spear / Fist) in "New Scene.unity" next to the package folder. Re-runnable.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BlockBuddyWeaponsSetup
{
    const string Folder = "/BlockBuddyWeapons/";

    // Data/BlockBuddyWeapons_Clips.json (only the fields the code needs)
    [Serializable] class ClipsJson { public WeaponsJson weapons; public ClipJson[] clips; }
    [Serializable] class WeaponsJson { public WeaponJson Sword, Bow, Spear, Fist; }
    [Serializable] class WeaponJson { public string hand, idle, run, skill; public string[] attacks; public ModelsJson models; }
    [Serializable] class ModelsJson { public string right, left; }
    [Serializable] class ClipJson { public string name, file; public float[] hits; public string[] hitHands; public ArrowJson[] arrows; }
    [Serializable] class ArrowJson { public float nock, release; }

    // ".../BlockBuddyWeapons/", found from the json so the package can live anywhere
    static string Root()
    {
        foreach (var g in AssetDatabase.FindAssets("BlockBuddyWeapons_Clips"))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.EndsWith(".json") && p.Contains(Folder)) return p.Substring(0, p.IndexOf(Folder) + Folder.Length);
        }
        throw new FileNotFoundException("BlockBuddyWeapons: Data/BlockBuddyWeapons_Clips.json not found");
    }

    [MenuItem("BlockBuddyWeapons/Create Weapon Data")]
    public static void CreateWeaponData()
    {
        string root = Root();
        var json = JsonUtility.FromJson<ClipsJson>(File.ReadAllText(root + "Data/BlockBuddyWeapons_Clips.json"));
        var clips = json.clips.ToDictionary(c => c.name);
        foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
        {
            var w = (WeaponJson)typeof(WeaponsJson).GetField(type.ToString()).GetValue(json.weapons);
            string path = root + "Data/WeaponData_" + type + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            bool isNew = !data;
            if (isNew) data = ScriptableObject.CreateInstance<WeaponData>();
            data.type = type;
            data.hand = (Hand)Enum.Parse(typeof(Hand), w.hand);
            data.modelRight = LoadModel(root, w.models?.right);
            data.modelLeft = LoadModel(root, w.models?.left);
            data.idle = LoadClip(root, clips[w.idle]);
            data.run = LoadClip(root, clips[w.run]);
            data.attacks = w.attacks.Select(a => LoadClip(root, clips[a])).ToArray();
            data.skill = LoadClip(root, clips[w.skill]);
            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);
            Debug.Log($"BlockBuddyWeapons: {path} ({data.attacks.Length} attacks)");
        }
        AssetDatabase.SaveAssets();
    }

    // JsonUtility leaves a missing hand as "" (models.right / models.left may be absent)
    static GameObject LoadModel(string root, string file) =>
        string.IsNullOrEmpty(file) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(root + file);

    static WeaponClip LoadClip(string root, ClipJson c) => new WeaponClip
    {
        clip = AssetDatabase.LoadAllAssetsAtPath(root + c.file).OfType<AnimationClip>().First(a => !a.name.StartsWith("__preview__")),
        hits = c.hits ?? new float[0],
        hitHands = (c.hitHands ?? new string[0]).Select(h => h == "L" ? Hand.Left : Hand.Right).ToArray(),
        arrows = (c.arrows ?? new ArrowJson[0]).Select(a => new Vector2(a.nock, a.release)).ToArray(),
    };

    static WeaponData[] LoadWeaponData(string root) => Enum.GetValues(typeof(WeaponType)).Cast<WeaponType>()
        .Select(t => AssetDatabase.LoadAssetAtPath<WeaponData>(root + "Data/WeaponData_" + t + ".asset")).ToArray();

    [MenuItem("BlockBuddyWeapons/Build Demo Scene")]
    public static void BuildDemoScene()
    {
        string root = Root();
        string parent = root.Substring(0, root.Length - "BlockBuddyWeapons/".Length);
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(parent + "New Scene.unity", OpenSceneMode.Single);
        // load assets after OpenScene: opening a scene unloads assets nothing references yet
        var weapons = LoadWeaponData(root);
        if (weapons.Any(w => !w)) { CreateWeaponData(); weapons = LoadWeaponData(root); }
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Ground" || go.name.StartsWith("Fighter_")) UnityEngine.Object.DestroyImmediate(go);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(2f, 1f, 2f);
        var groundRenderer = ground.GetComponent<Renderer>();
        groundRenderer.sharedMaterial = new Material(groundRenderer.sharedMaterial) { name = "Ground_Gray", color = new Color(0.55f, 0.55f, 0.55f) };

        var cam = scene.GetRootGameObjects().Select(g => g.GetComponentInChildren<Camera>()).FirstOrDefault(c => c);
        if (cam)
        {
            cam.transform.position = new Vector3(9f, 6.5f, 11f);
            cam.transform.LookAt(new Vector3(0f, 1f, 0f));
            cam.fieldOfView = 35f;
        }

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(parent + "BlockBuddy/Models/BlockBuddy.fbx");
        var arrow = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Models/Weapon_Arrow.fbx");
        string[] skins = { "Skin_GreenJacket", "Skin_CyanShirt" };
        int count = weapons.Length;   // one fighter per weapon: Sword / Bow / Spear / Fist
        for (int i = 0; i < count; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            go.name = "Fighter_" + (i + 1);
            go.transform.SetPositionAndRotation(new Vector3(3f * (i - (count - 1) * 0.5f), 0f, 0f), Quaternion.identity);
            ApplySkin(go, AssetDatabase.LoadAssetAtPath<Texture2D>(parent + "BlockBuddy/Textures/" + skins[i % skins.Length] + ".png"));

            var animator = go.AddComponent<Animator>();   // no controller: BlockBuddyAnimPlayer drives it through a PlayableGraph
            animator.applyRootMotion = false;
            go.AddComponent<BlockBuddyAnimPlayer>();
            go.AddComponent<WeaponHolder>().arrowModel = arrow;
            var fighter = go.AddComponent<BlockBuddyFighter>();
            fighter.weapons = weapons;
            fighter.startWeapon = (WeaponType)i;
            fighter.startOffset = 0.7f * i;

            var label = new GameObject("Label", typeof(TextMesh), typeof(FighterLabel));
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            // face the camera already in the saved scene (FighterLabel keeps it facing at runtime)
            if (cam) label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position, Vector3.up);
            var tm = label.GetComponent<TextMesh>();
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 48;
            tm.characterSize = 0.05f;
            tm.text = fighter.startWeapon.ToString();
            label.GetComponent<FighterLabel>().fighter = fighter;
        }
        EditorSceneManager.SaveScene(scene);
        Debug.Log("BlockBuddyWeapons: demo scene built in " + scene.path);
    }

    // clone the skin material and swap its texture (like BlockGymScene.ApplySkin)
    static void ApplySkin(GameObject go, Texture2D tex)
    {
        if (!tex) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            r.sharedMaterial = new Material(r.sharedMaterial) { name = "BlockBuddy_Skin_" + tex.name, mainTexture = tex };
    }
}
