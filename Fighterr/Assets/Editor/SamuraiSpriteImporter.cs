using UnityEditor;
using UnityEngine;

namespace SamuraiFighter.EditorTools
{
    public class SamuraiSpriteImporter : AssetPostprocessor
    {
        private const string TargetRoot = "Assets/Art/Characters/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(TargetRoot)) return;
            if (!assetPath.EndsWith(".png")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
