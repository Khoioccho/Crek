#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PostApoc
{
    // Builds the first-person arms controller from FPArms.fbx clips
    // (Sword_Idle / Sword_Walk blended on Speed, Sword_Slash1 on the Attack trigger).
    [InitializeOnLoad]
    public static class PAFPArmsAnim
    {
        const string Fbx = "Assets/Resources/Models/FPArms.fbx";
        const string Out = "Assets/Resources/Models/FPArmsAnim.controller";
        static int _tries;

        static PAFPArmsAnim() { EditorApplication.delayCall += Build; }

        static AnimationClip Clip(string suffix)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Fbx))
            {
                var c = o as AnimationClip;
                if (c != null && !c.name.StartsWith("__preview__") && c.name.EndsWith(suffix)) return c;
            }
            return null;
        }

        static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(Out)) return;
            if (!File.Exists(Fbx)) return;

            var idle = Clip("Sword_Idle");
            var slash = Clip("Sword_Slash1");
            if (idle == null || slash == null)
            {
                if (_tries++ < 15) EditorApplication.delayCall += Build; // still importing
                return;
            }

            try
            {
                var walk = Clip("Sword_Walk");

                var ac = AnimatorController.CreateAnimatorControllerAtPath(Out);
                ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
                ac.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                var sm = ac.layers[0].stateMachine;

                BlendTree bt;
                var move = ac.CreateBlendTreeInController("Move", out bt, 0);
                bt.blendType = BlendTreeType.Simple1D;
                bt.blendParameter = "Speed";
                bt.useAutomaticThresholds = false;
                bt.AddChild(idle, 0f);
                if (walk != null) bt.AddChild(walk, 2f);
                sm.defaultState = move;

                var atk = sm.AddState("Slash"); atk.motion = slash;
                var any = sm.AddAnyStateTransition(atk);
                any.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                any.duration = 0.03f; any.canTransitionToSelf = false;
                var ex = atk.AddTransition(move); ex.hasExitTime = true; ex.exitTime = 0.6f; ex.duration = 0.1f;

                EditorUtility.SetDirty(ac);
                AssetDatabase.SaveAssets();
                Debug.Log("[PostApoc] Built FPArmsAnim.controller.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[PostApoc] FP arms controller skipped: " + e.Message);
            }
        }
    }
}
#endif
