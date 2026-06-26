using UnityEngine;

namespace PostApoc
{
    // Persisted game settings (PlayerPrefs).
    public static class PASettings
    {
        public static float Master, Music, Sfx, Sensitivity;
        static bool _loaded;

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            Master = PlayerPrefs.GetFloat("opt_master", 0.8f);
            Music = PlayerPrefs.GetFloat("opt_music", 0.6f);
            Sfx = PlayerPrefs.GetFloat("opt_sfx", 0.8f);
            Sensitivity = PlayerPrefs.GetFloat("opt_sens", 1f);
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("opt_master", Master);
            PlayerPrefs.SetFloat("opt_music", Music);
            PlayerPrefs.SetFloat("opt_sfx", Sfx);
            PlayerPrefs.SetFloat("opt_sens", Sensitivity);
            PlayerPrefs.Save();
        }

        public static void Adjust(int index, int dir)
        {
            float d = dir * 0.05f;
            if (index == 0) Master = Mathf.Clamp01(Master + d);
            else if (index == 1) Music = Mathf.Clamp01(Music + d);
            else if (index == 2) Sfx = Mathf.Clamp01(Sfx + d);
            else if (index == 3)
            {
                float t = Mathf.Clamp01(Mathf.InverseLerp(0.2f, 2.5f, Sensitivity) + d);
                Sensitivity = Mathf.Lerp(0.2f, 2.5f, t);
            }
        }
    }

    // Plays the looping BGM (Resources/Audio/BGM) and one-shot SFX, scaled by the
    // Master/Music/SFX settings. Lives on the [Game] object.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I;
        AudioSource _bgm;
        static AudioClip _whoosh, _thud;

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            PASettings.Load();

            var clip = Resources.Load<AudioClip>("Audio/BGM");
            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.clip = clip;
            _bgm.loop = true;
            _bgm.playOnAwake = false;
            _bgm.spatialBlend = 0f;
            ApplyVolumes();
            if (clip != null) _bgm.Play();
            else Debug.LogWarning("[PostApoc] No BGM found at Resources/Audio/BGM — music silent.");
        }

        public void ApplyVolumes()
        {
            if (_bgm != null) _bgm.volume = Mathf.Clamp01(PASettings.Master * PASettings.Music);
        }

        public static void Sfx(AudioClip c, Vector3 pos, float vol = 1f)
        {
            if (c == null) return;
            PASettings.Load();
            float v = Mathf.Clamp01(vol * PASettings.Master * PASettings.Sfx);
            if (v <= 0.002f) return;
            AudioSource.PlayClipAtPoint(c, pos, v);
        }

        // ---- tiny procedural SFX (no audio assets needed) --------------------

        public static AudioClip Whoosh { get { if (_whoosh == null) _whoosh = GenWhoosh(); return _whoosh; } }
        public static AudioClip Thud { get { if (_thud == null) _thud = GenThud(); return _thud; } }

        // Imported voice clips (Resources/Audio/*). Null-safe if missing.
        static AudioClip _villagerVoice, _skeletonVoice;
        public static AudioClip VillagerVoice
        { get { if (_villagerVoice == null) _villagerVoice = Resources.Load<AudioClip>("Audio/VillagerVoice"); return _villagerVoice; } }
        public static AudioClip SkeletonVoice
        { get { if (_skeletonVoice == null) _skeletonVoice = Resources.Load<AudioClip>("Audio/SkeletonVoice"); return _skeletonVoice; } }

        static AudioClip GenWhoosh()
        {
            int sr = 44100; float dur = 0.22f;
            int n = (int)(sr * dur);
            var d = new float[n];
            float lp = 0f;
            var rnd = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.18f;                       // soften to an "air" hiss
                float env = Mathf.Sin(t * Mathf.PI);
                d[i] = lp * env * env * 0.9f;
            }
            var c = AudioClip.Create("whoosh", n, 1, sr, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip GenThud()
        {
            int sr = 44100; float dur = 0.18f;
            int n = (int)(sr * dur);
            var d = new float[n];
            var rnd = new System.Random(13);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float body = Mathf.Sin(2f * Mathf.PI * 85f * t * (1f - t * 2f)) * Mathf.Exp(-16f * t);
                float crack = (float)(rnd.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-45f * t) * 0.4f;
                d[i] = (body + crack) * 0.9f;
            }
            var c = AudioClip.Create("thud", n, 1, sr, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
