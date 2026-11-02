// Import settings for everything inside a folder named "BlockKit". Runs automatically on import.
// Keep this file inside an "Editor" folder.
using UnityEditor;
using UnityEngine;

public class BlockKitImport : AssetPostprocessor
{
    const string Folder = "/BlockKit/";
    static bool IsBlockKit(string path) => path.Replace('\\', '/').Contains(Folder);
    static string Root(string path) { path = path.Replace('\\', '/'); return path.Substring(0, path.IndexOf(Folder) + Folder.Length); }

    // Pixel textures: crisp, uncompressed. Wood / brick tile, the palette does not.
    void OnPreprocessTexture()
    {
        if (!IsBlockKit(assetPath)) return;
        var ti = (TextureImporter)assetImporter;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.wrapMode = assetPath.Contains("Palette") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
    }

    // Props and the scene: static meshes, materials remapped to Materials/*.mat (BlockKit/Flat shader)
    void OnPreprocessModel()
    {
        if (!IsBlockKit(assetPath)) return;
        var mi = (ModelImporter)assetImporter;
        mi.animationType = ModelImporterAnimationType.None;
        mi.importAnimation = false;
        foreach (var name in new[] { "BlockKit_Palette", "BlockKit_Wood", "BlockKit_Brick" })
        {
            string matPath = Root(assetPath) + "Materials/" + name + ".mat";
#if UNITY_2020_2_OR_NEWER
            context.DependsOnSourceAsset(matPath);
#endif
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), mat);
        }
    }
}
