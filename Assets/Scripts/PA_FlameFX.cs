using UnityEngine;

namespace PostApoc
{
    // Self-contained stylized campfire flame: emissive flame shapes that flicker, rising
    // embers, and a flickering point light. Uses URP/Lit emissive (renders reliably) and is
    // always upright — unlike the imported flipbook-flame FBX.
    public class FlameFX : MonoBehaviour
    {
        Transform _flame;
        Transform[] _embers;
        float[] _ep;
        Light _light;

        void Start()
        {
            var outer = PAArt.MatEmissive(new Color(1f, 0.42f, 0.10f), 3.0f);
            var inner = PAArt.MatEmissive(new Color(1f, 0.82f, 0.28f), 4.5f);

            _flame = new GameObject("Flame").transform;
            _flame.SetParent(transform, false);
            _flame.localPosition = new Vector3(0f, 0.35f, 0f);

            Strip(PAArt.Sph(_flame, new Vector3(0, 0.25f, 0), new Vector3(0.55f, 1.2f, 0.55f), outer));
            Strip(PAArt.Sph(_flame, new Vector3(0, 0.40f, 0), new Vector3(0.32f, 0.85f, 0.32f), inner));
            for (int i = 0; i < 3; i++)
            {
                float a = i / 3f * Mathf.PI * 2f;
                Strip(PAArt.Sph(_flame, new Vector3(Mathf.Cos(a) * 0.22f, 0.15f, Mathf.Sin(a) * 0.22f),
                    new Vector3(0.22f, 0.6f, 0.22f), outer));
            }

            var emberMat = PAArt.MatEmissive(new Color(1f, 0.6f, 0.2f), 3f);
            _embers = new Transform[7];
            _ep = new float[7];
            for (int i = 0; i < _embers.Length; i++)
            {
                var e = PAArt.Sph(transform, Vector3.zero, new Vector3(0.06f, 0.06f, 0.06f), emberMat);
                Strip(e);
                _embers[i] = e.transform;
                _ep[i] = Random.value;
            }

            var lg = new GameObject("Fire Light");
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = new Vector3(0, 1f, 0);
            _light = lg.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.55f, 0.2f);
            _light.range = 14f;
            _light.intensity = 3f;
        }

        static void Strip(GameObject go) { PAArt.StripColliders(go); }

        void Update()
        {
            float t = Time.time;
            float n = Mathf.PerlinNoise(t * 4f, 0.3f);

            if (_flame != null)
            {
                _flame.localScale = new Vector3(
                    0.9f + Mathf.PerlinNoise(t * 5f, 1f) * 0.25f,
                    0.9f + n * 0.4f,
                    0.9f + Mathf.PerlinNoise(t * 5f, 2f) * 0.25f);
                _flame.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 2f) * 6f, Mathf.Sin(t * 3f) * 5f);
            }
            if (_light != null) _light.intensity = 2.4f + n * 1.6f;

            for (int i = 0; i < _embers.Length; i++)
            {
                _ep[i] += Time.deltaTime * 0.45f;
                if (_ep[i] > 1f) _ep[i] -= 1f;
                float p = _ep[i];
                float ang = i * 2.3f;
                float r = 0.18f * (1f - p);
                _embers[i].localPosition = new Vector3(
                    Mathf.Cos(ang + p * 6f) * r, 0.4f + p * 1.7f, Mathf.Sin(ang + p * 6f) * r);
                _embers[i].localScale = Vector3.one * 0.07f * (1f - p);
            }
        }
    }
}
