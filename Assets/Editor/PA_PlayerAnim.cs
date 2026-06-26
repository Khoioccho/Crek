#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PostApoc
{
    // Builds the player's Humanoid Animator controller from the Basic Motions clips
    // (Idle/Walk/Run blend on Speed) plus Attack (slash pack), Jump and Death.
    // These are Humanoid clips, so they retarget onto the warrior's Humanoid avatar.
    [InitializeOnLoad]
    public static class PAPlayerAnim
    {
        const string Out = "Assets/Resources/Models/PlayerAnim.controller";
        const string BM = "Assets/PolyOne/Basic Motions/Animation/";
        const string ATK = "Assets/Free Slash VFX/Demo/Animation/Sword Attack 1.anim";
        const string ROLL = "Assets/VanillaLoopStudio/FreeSampleAnimationSet/Art/Animations/RollDodgeDashSet/Mannequin/RootMotion/Roll/A_Roll_IdleFwd.fbx";
        static int _tries;

        static PAPlayerAnim() { EditorApplication.delayCall += Build; }

        static AnimationClip C(string p) => AssetDatabase.LoadAssetAtPath<AnimationClip>(p);

        static AnimationClip FromFbx(string path)
        {
            if (!File.Exists(path)) return null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var c = o as AnimationClip;
                if (c != null && !c.name.StartsWith("__preview__")) return c;
            }
            return null;
        }

        // If the controller already exists (built before the roll pack arrived), add the
        // Roll state to it instead of skipping.
        static void Upgrade()
        {
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(Out);
            if (ac == null) return;
            foreach (var p in ac.parameters) if (p.name == "Roll") return; // already upgraded
            var roll = FromFbx(ROLL);
            if (roll == null) { if (_tries++ < 15) EditorApplication.delayCall += Build; return; }

            ac.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
            var sm = ac.layers[0].stateMachine;
            AnimatorState move = null;
            foreach (var s in sm.states) if (s.state.name == "Move") move = s.state;

            var st = sm.AddState("Roll"); st.motion = roll;
            var any = sm.AddAnyStateTransition(st);
            any.AddCondition(AnimatorConditionMode.If, 0, "Roll");
            any.duration = 0.05f; any.canTransitionToSelf = false;
            if (move != null)
            {
                var ex = st.AddTransition(move);
                ex.hasExitTime = true; ex.exitTime = 0.8f; ex.duration = 0.1f;
            }
            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            Debug.Log("[PostApoc] Added Roll state to PlayerAnim.controller.");
        }

        static void EnsureLoop(AnimationClip c)
        {
            if (c == null) return;
            var s = AnimationUtility.GetAnimationClipSettings(c);
            if (!s.loopTime) { s.loopTime = true; AnimationUtility.SetAnimationClipSettings(c, s); EditorUtility.SetDirty(c); }
        }

        static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(Out)) { Upgrade(); return; }

            var idle = C(BM + "Idle.anim"); var walk = C(BM + "Walk.anim"); var run = C(BM + "Run.anim");
            if (idle == null || walk == null || run == null)
            {
                if (_tries++ < 15) EditorApplication.delayCall += Build; // still importing
                return;
            }

            try
            {
                EnsureLoop(idle); EnsureLoop(walk); EnsureLoop(run);
                var jump = C(BM + "Jumping Up.anim");
                var die = C(BM + "Dying.anim");
                var atk = C(ATK);

                if (!Directory.Exists("Assets/Resources/Models")) Directory.CreateDirectory("Assets/Resources/Models");

                var ac = AnimatorController.CreateAnimatorControllerAtPath(Out);
                ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
                ac.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                ac.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
                ac.AddParameter("Die", AnimatorControllerParameterType.Trigger);

                var sm = ac.layers[0].stateMachine;

                BlendTree bt;
                var move = ac.CreateBlendTreeInController("Move", out bt, 0);
                bt.blendType = BlendTreeType.Simple1D;
                bt.blendParameter = "Speed";
                bt.useAutomaticThresholds = false;
                bt.AddChild(idle, 0f);
                bt.AddChild(walk, 4.2f);
                bt.AddChild(run, 7.2f);
                sm.defaultState = move;

                if (atk != null)
                {
                    var s = sm.AddState("Attack"); s.motion = atk;
                    var any = sm.AddAnyStateTransition(s);
                    any.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                    any.duration = 0.05f; any.canTransitionToSelf = false;
                    var ex = s.AddTransition(move); ex.hasExitTime = true; ex.exitTime = 0.7f; ex.duration = 0.15f;
                }
                if (jump != null)
                {
                    var s = sm.AddState("Jump"); s.motion = jump;
                    var any = sm.AddAnyStateTransition(s);
                    any.AddCondition(AnimatorConditionMode.If, 0, "Jump");
                    any.duration = 0.05f; any.canTransitionToSelf = false;
                    var ex = s.AddTransition(move); ex.hasExitTime = true; ex.exitTime = 0.8f; ex.duration = 0.15f;
                }
                if (die != null)
                {
                    var s = sm.AddState("Death"); s.motion = die;
                    var any = sm.AddAnyStateTransition(s);
                    any.AddCondition(AnimatorConditionMode.If, 0, "Die");
                    any.duration = 0.1f; any.canTransitionToSelf = false;
                }
                var rollClip = FromFbx(ROLL);
                if (rollClip != null)
                {
                    ac.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
                    var s = sm.AddState("Roll"); s.motion = rollClip;
                    var any = sm.AddAnyStateTransition(s);
                    any.AddCondition(AnimatorConditionMode.If, 0, "Roll");
                    any.duration = 0.05f; any.canTransitionToSelf = false;
                    var ex = s.AddTransition(move);
                    ex.hasExitTime = true; ex.exitTime = 0.8f; ex.duration = 0.1f;
                }

                EditorUtility.SetDirty(ac);
                AssetDatabase.SaveAssets();
                Debug.Log("[PostApoc] Built PlayerAnim.controller (locomotion + attack/jump/death).");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[PostApoc] Player controller skipped: " + e.Message);
            }
        }
    }
}
#endif
