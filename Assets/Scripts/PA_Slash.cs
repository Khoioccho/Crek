using UnityEngine;

namespace PostApoc
{
    // Animates a spawned slash crescent: sweeps around the view axis and fades out, then
    // destroys itself (and its material).
    public class SlashFade : MonoBehaviour
    {
        public float life = 0.18f;
        public float spin = 800f;
        float _t, _baseScale;
        Material _mat;
        Color _c0;

        public void Init(Material m, float spinDir)
        {
            _mat = m;
            _c0 = (m != null && m.HasProperty("_BaseColor")) ? m.GetColor("_BaseColor") : Color.white;
            spin = Mathf.Abs(spin) * Mathf.Sign(spinDir);
            _baseScale = transform.localScale.x;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = _t / life;
            if (k >= 1f)
            {
                if (_mat != null) Destroy(_mat);
                Destroy(gameObject);
                return;
            }
            transform.Rotate(0f, 0f, spin * Time.deltaTime, Space.Self);
            if (_mat != null && _mat.HasProperty("_BaseColor"))
            {
                var c = _c0; c.a = 1f - k;
                _mat.SetColor("_BaseColor", c);
            }
            transform.localScale = Vector3.one * _baseScale * (1f + k * 0.35f);
        }
    }
}
