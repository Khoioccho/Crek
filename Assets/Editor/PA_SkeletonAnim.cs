#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PostApoc
{
    // Builds an Animator controller for the SazenGames skeleton from its bundled clips
    // (idle / walk / slash / death), driven by a "Speed" float and "Attack"/"Die" triggers.
    // Saved under Resources so the enemy can load + assign it at runtime.
    [InitializeOnLoad]
    public static class PASkeletonAnim
    {
        const string AnimDir = "Assets/SazenGames/Skeleton/Art/Animations/";
        const string Out = "Assets/Resources/Models/SkeletonAnim.controller";
        static int _tries;

        static PASkeletonAnim() { EditorApplication.delayCall += Build; }

        static AnimationClip Clip(string fbx)
        {
            string path = AnimDir + fbx + ".fbx";
            if (!File.Exists(path)) return null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var c = o as AnimationClip;
                if (c != null && !c.name.StartsWith("__preview__")) return c;
            }
            return null;
        }

        static void SetLoop(string fbx)
        {
            var mi = AssetImporter.GetAtPath(AnimDir + fbx + ".fbx") as ModelImporter;
            if (mi == null) return;
            var clips = mi.clipAnimations;
            if (clips == null || clips.Length == 0) clips = mi.defaultClipAnimations;
            bool ch = false;
            for (int i = 0; i < clips.Length; i++) if (!clips[i].loopTime) { clips[i].loopTime = true; ch = true; }
            if (ch) { mi.clipAnimations = clips; mi.SaveAndReimport(); }
        }

        static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(Out)) return;
            if (!File.Exists(AnimDir + "Skeleton_idle.fbx")) return; // package not installed

            if (Clip("Skeleton_idle") == null || Clip("Skeleton_walk_forward") == null)
            {
                if (_tries++ < 15) EditorApplication.delayCall += Build; // clips still importing
                return;
            }

            try
            {
                SetLoop("Skeleton_idle");
                SetLoop("Skeleton_walk_forward");
                var idle = Clip("Skeleton_idle");
                var walk = Clip("Skeleton_walk_forward");
                var slash = Clip("Skeleton_slash01");
                var death = Clip("Skeleton_death");
                if (idle == null || walk == null) return;

                if (!Directory.Exists("Assets/Resources/Models"))
                    Directory.CreateDirectory("Assets/Resources/Models");

                var ac = AnimatorController.CreateAnimatorControllerAtPath(Out);
                ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
                ac.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                ac.AddParameter("Die", AnimatorControllerParameterType.Trigger);

                var sm = ac.layers[0].stateMachine;
                var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
                var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
                sm.defaultState = sIdle;

                var toWalk = sIdle.AddTransition(sWalk);
                toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                toWalk.hasExitTime = false; toWalk.duration = 0.15f;

                var toIdle = sWalk.AddTransition(sIdle);
                toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
                toIdle.hasExitTime = false; toIdle.duration = 0.15f;

                if (slash != null)
                {
                    var sAtk = sm.AddState("Attack"); sAtk.motion = slash;
                    var anyAtk = sm.AddAnyStateTransition(sAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                    anyAtk.duration = 0.1f; anyAtk.canTransitionToSelf = false;
                    var atkOut = sAtk.AddTransition(sIdle);
                    atkOut.hasExitTime = true; atkOut.exitTime = 0.85f; atkOut.duration = 0.1f;
                }
                if (death != null)
                {
                    var sDie = sm.AddState("Death"); sDie.motion = death;
                    var anyDie = sm.AddAnyStateTransition(sDie);
                    anyDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
                    anyDie.duration = 0.1f; anyDie.canTransitionToSelf = false;
                }

                EditorUtility.SetDirty(ac);
                AssetDatabase.SaveAssets();
                Debug.Log("[PostApoc] Built SkeletonAnim.controller (idle/walk/attack/death).");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[PostApoc] Skeleton controller build skipped: " + e.Message +
                                 " (skeletons will still play their built-in idle).");
            }
        }
    }
}
#endif
