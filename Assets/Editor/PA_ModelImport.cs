#if UNITY_EDITOR
using UnityEditor;

namespace PostApoc
{
    // Imported models dropped in Resources/Models get their materials built through
    // Unity's MaterialDescription path, which (under URP) produces correct URP/Lit
    // materials with their textures hooked up — instead of pink built-in materials.
    public class PAModelImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Models/")) return;
            var mi = assetImporter as ModelImporter;
            if (mi == null) return;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }
    }
}
#endif
