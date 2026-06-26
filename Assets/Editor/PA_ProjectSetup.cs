#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PostApoc
{
    // Runs automatically when the project loads in the Editor.
    // The project was created from the URP "2D" template (its URP asset uses a 2D
    // Renderer), but this game is 3D. This swaps the active URP asset's renderer for a
    // 3D Universal Renderer so meshes get real lighting and shadows.
    //
    // It reuses the project's existing, fully-configured URP asset and only changes the
    // renderer reference (via SerializedObject), so it depends on nothing fragile and is
    // wrapped in try/catch — it can never block Play mode.
    [InitializeOnLoad]
    public static class PAProjectSetup
    {
        const string RendererPath = "Assets/Settings/PA_Renderer3D.asset";
        const string ExistingUrpPath = "Assets/Settings/UniversalRP.asset";
        const string NewUrpPath = "Assets/Settings/PA_URP3D.asset";

        static PAProjectSetup()
        {
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { Ensure3DRenderer(); }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    "[PostApoc] Auto 3D setup skipped (" + e.Message + ").\n" +
                    "The game still runs, but if it looks flat, open Project Settings > Graphics / Quality " +
                    "and assign a URP asset whose renderer is a 'Universal Renderer' (not the 2D Renderer).");
            }
        }

        static void Ensure3DRenderer()
        {
            if (!Directory.Exists("Assets/Settings"))
                Directory.CreateDirectory("Assets/Settings");

            // 1) Ensure a 3D Universal Renderer data asset exists.
            var renderer3D = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer3D == null)
            {
                renderer3D = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer3D, RendererPath);
                AssetDatabase.SaveAssets();
            }

            // 2) Find the URP asset the project actually uses.
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ExistingUrpPath);
            if (urp == null)
            {
                urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(NewUrpPath);
                if (urp == null)
                {
                    urp = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                    AssetDatabase.CreateAsset(urp, NewUrpPath);
                    AssetDatabase.SaveAssets();
                }
                AssignPipeline(urp);
            }

            // 3) Point the URP asset's renderer list at the 3D renderer (if not already).
            var so = new SerializedObject(urp);
            var list = so.FindProperty("m_RendererDataList");
            if (list == null) return;

            bool already = list.arraySize > 0 &&
                           list.GetArrayElementAtIndex(0).objectReferenceValue is UniversalRendererData;
            if (already) return;

            if (list.arraySize < 1) list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer3D;
            var defaultIdx = so.FindProperty("m_DefaultRendererIndex");
            if (defaultIdx != null) defaultIdx.intValue = 0;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();

            Debug.Log("[PostApoc] Project configured for 3D (Universal Renderer). " +
                      "Open Assets/Scenes/SampleScene.unity and press Play.");
        }

        static void AssignPipeline(UniversalRenderPipelineAsset urp)
        {
            if (GraphicsSettings.defaultRenderPipeline != urp)
                GraphicsSettings.defaultRenderPipeline = urp;

            int current = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline != urp)
                    QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
