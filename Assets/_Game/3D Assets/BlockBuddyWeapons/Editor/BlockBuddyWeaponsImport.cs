// Import settings for everything inside a folder named "BlockBuddyWeapons". Runs automatically on import.
// Keep this file inside an "Editor" folder. Mirrors BlockBuddy/Editor/BlockBuddyImport.cs.
using UnityEditor;
using UnityEngine;

public class BlockBuddyWeaponsImport : AssetPostprocessor
{
    const string Folder = "/BlockBuddyWeapons/";
    const string MatName = "BlockBuddyWeapons_Mat";
    static bool IsOurs(string path) => path.Replace('\\', '/').Contains(Folder);
    static string Root(string path) { path = path.Replace('\\', '/'); return path.Substring(0, path.IndexOf(Folder) + Folder.Length); }

    // Pixel-art atlas: crisp, uncompressed, keep alpha
    void OnPreprocessTexture()
    {
        if (!IsOurs(assetPath)) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.textureShape = TextureImporterShape.Texture2D;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
    }

    void OnPreprocessModel()
    {
        if (!IsOurs(assetPath)) return;
        var mi = (ModelImporter)assetImporter;
        if (assetPath.Contains("@"))
        {
            // animation clip files: same Generic rig as BlockBuddy.fbx, no materials
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            return;
        }
        // weapon models: static meshes, material remapped to Materials/BlockBuddyWeapons_Mat.mat
        mi.animationType = ModelImporterAnimationType.None;
        mi.importAnimation = false;
        string matPath = Root(assetPath) + "Materials/" + MatName + ".mat";
#if UNITY_2020_2_OR_NEWER
        context.DependsOnSourceAsset(matPath);
#endif
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat != null) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), MatName), mat);
    }

    // *_Idle and *_Run loop; attacks / skills / shots play once. First import only.
    void OnPreprocessAnimation()
    {
        if (!IsOurs(assetPath)) return;
        var mi = (ModelImporter)assetImporter;
        if (mi.clipAnimations.Length > 0) return;
        var clips = mi.defaultClipAnimations;
        foreach (var c in clips) c.loopTime = c.name.EndsWith("_Idle") || c.name.EndsWith("_Run");
        mi.clipAnimations = clips;
    }
}
