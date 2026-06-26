using UnityEngine;

namespace PostApoc
{
    // First-person viewmodel: arms + sword parented to the camera, playing the FP melee
    // clips. Shown only in first person. Placement is tunable (these models come from an
    // external pack, so the offsets below may need a tweak once you see it).
    public class FirstPersonArms : MonoBehaviour
    {
        [Header("Arms placement (relative to camera)")]
        public Vector3 armsLocalPos = new Vector3(0f, -0.45f, 0.42f);
        public Vector3 armsLocalEuler = new Vector3(0f, 180f, 0f);
        public float armsScale = 1f;

        [Header("Sword placement (relative to hand bone)")]
        public Vector3 swordLocalPos = Vector3.zero;
        public Vector3 swordLocalEuler = Vector3.zero;
        public float swordScale = 1f;

        GameObject _arms;
        Animator _anim;
        bool _aSpeed, _aAttack, _visible = true;

        public void Init(Transform cam)
        {
            var prefab = Resources.Load<GameObject>("Models/FPArms");
            if (prefab == null) return;

            _arms = Instantiate(prefab, cam, false);
            _arms.transform.localPosition = armsLocalPos;
            _arms.transform.localEulerAngles = armsLocalEuler;
            _arms.transform.localScale = Vector3.one * armsScale;

            _anim = _arms.GetComponentInChildren<Animator>();
            if (_anim != null)
            {
                if (_anim.GetComponent<PAAnimEvents>() == null) _anim.gameObject.AddComponent<PAAnimEvents>();
                var ctrl = Resources.Load<RuntimeAnimatorController>("Models/FPArmsAnim");
                if (ctrl != null) _anim.runtimeAnimatorController = ctrl;
                _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var p in _anim.parameters)
                {
                    if (p.name == "Speed") _aSpeed = true;
                    else if (p.name == "Attack") _aAttack = true;
                }
            }

            // attach the sword to the hand bone if we can find it
            var swordPrefab = Resources.Load<GameObject>("Models/FPSword");
            if (swordPrefab != null)
            {
                var hand = FindHand(_arms.transform);
                var parent = hand != null ? hand : _arms.transform;
                var sword = Instantiate(swordPrefab, parent, false);
                sword.transform.localPosition = swordLocalPos;
                sword.transform.localEulerAngles = swordLocalEuler;
                sword.transform.localScale = Vector3.one * swordScale;
            }
        }

        static Transform FindHand(Transform root)
        {
            Transform any = null;
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLower();
                if (n.Contains("hand"))
                {
                    if (n.Contains("r")) return t;   // prefer right hand
                    any = t;
                }
            }
            if (any != null) return any;
            foreach (var t in root.GetComponentsInChildren<Transform>())
                if (t.name.ToLower().Contains("wrist")) return t;
            return null;
        }

        public void SetVisible(bool v)
        {
            if (_arms == null || _visible == v) return;
            _visible = v;
            _arms.SetActive(v);
        }

        public void SetSpeed(float s) { if (_anim != null && _aSpeed) _anim.SetFloat("Speed", s); }
        public void Attack() { if (_anim != null && _aAttack && _visible) _anim.SetTrigger("Attack"); }
    }
}
