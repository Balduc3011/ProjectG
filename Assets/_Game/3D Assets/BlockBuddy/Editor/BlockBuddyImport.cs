// Import settings for everything inside a folder named "BlockBuddy". Runs automatically on import.
// Keep this file inside an "Editor" folder.
using UnityEditor;
using UnityEngine;

public class BlockBuddyImport : AssetPostprocessor
{
    const string Folder = "/BlockBuddy/";
    static bool IsBlockBuddy(string path) => path.Replace('\\', '/').Contains(Folder);
    static string MaterialPath(string path)
    {
        path = path.Replace('\\', '/');
        return path.Substring(0, path.IndexOf(Folder) + Folder.Length) + "Materials/BlockBuddy_Skin.mat";
    }

    // Pixel-art skins: crisp, uncompressed, keep alpha
    void OnPreprocessTexture()
    {
        if (!IsBlockBuddy(assetPath)) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureShape = TextureImporterShape.Texture2D;   // minimal .meta could come in as Cube
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
    }

    void OnPreprocessModel()
    {
        if (!IsBlockBuddy(assetPath)) return;
        var mi = (ModelImporter)assetImporter;
        mi.animationType = ModelImporterAnimationType.Generic;
        if (assetPath.Contains("@"))
        {
            mi.materialImportMode = ModelImporterMaterialImportMode.None;   // clip files: no materials
            return;
        }
        // model: use Materials/BlockBuddy_Skin.mat (BlockBuddy/Skin shader) instead of the FBX material
        string matPath = MaterialPath(assetPath);
#if UNITY_2020_2_OR_NEWER
        context.DependsOnSourceAsset(matPath);
#endif
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat != null)
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "BlockBuddy_Skin"), mat);
    }

    // Cycles loop; transition clips (lie down / get up / pick up / put down) play once.
    // Applied on first import only, later Inspector edits are kept.
    static readonly string[] OneShot = { "_LieDown", "_GetUp", "_PickUp", "_PutDown" };
    void OnPreprocessAnimation()
    {
        if (!IsBlockBuddy(assetPath)) return;
        var mi = (ModelImporter)assetImporter;
        if (mi.clipAnimations.Length > 0) return;
        var clips = mi.defaultClipAnimations;
        foreach (var c in clips)
        {
            bool once = false;
            foreach (var s in OneShot) if (c.name.EndsWith(s)) once = true;
            c.loopTime = !once;
        }
        mi.clipAnimations = clips;
    }
}
