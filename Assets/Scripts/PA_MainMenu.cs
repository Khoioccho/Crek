using UnityEngine;

namespace PostApoc
{
    // The introduction screen: a post-apocalypse backdrop that drifts in the wind and
    // cross-fades to a new scene every 10 seconds (GTA load-screen style), with a Start button.
    // Drawn with IMGUI; clicks resolved via the new Input System so it works regardless.
    public class MainMenu : MonoBehaviour
    {
        const float Cycle = 10f;   // seconds per backdrop (as requested)
        const float Fade = 1.6f;   // cross-fade duration

        static readonly string[] SceneNames =
            { "Ashen Plains", "Ruins of the Old World", "Toxic Mire", "Frozen Silence" };

        static readonly Color[] GrassTints =
        {
            new Color(0.46f, 0.40f, 0.22f),
            new Color(0.30f, 0.18f, 0.16f),
            new Color(0.30f, 0.42f, 0.20f),
            new Color(0.60f, 0.66f, 0.74f),
        };

        Texture2D[] _bgs;
        Texture2D _grass, _vignette, _px;
        float _startTime;
        int _hover = -1;          // 0 = Start, 1 = Settings, 2 = Quit, 3 = Back
        int _selOpt;              // settings panel focus: 0-3 = sliders, 4 = Back
        Rect _rStart, _rQuit, _rSettings, _rBack, _rPanel;
        bool _showSettings;
        float _settingsOpenedAt;

        GUIStyle _title, _sub, _btn, _tag, _foot;
        bool _stylesBuilt;

        void OnEnable()
        {
            _startTime = Time.unscaledTime;
            if (_bgs == null) Generate();
        }

        void Generate()
        {
            _bgs = new Texture2D[4];
            for (int i = 0; i < 4; i++) _bgs[i] = PAArt.Background(i);
            _grass = PAArt.Grass();
            _vignette = PAArt.ThemeVignette();
            _px = PAArt.Pixel(Color.white);
        }

        void Update()
        {
            if (GameManager.Instance.State != GameState.Menu) return;
            Layout();

            Vector2 mp = PAInput.MouseGui();

            if (_showSettings)
            {
                int ny = PAInput.MenuNavY();
                if (ny != 0) _selOpt = Mathf.Clamp(_selOpt + ny, 0, 4);
                int nx = PAInput.MenuNavX();
                if (nx != 0 && _selOpt < 4) PASettings.Adjust(_selOpt, nx);

                bool overBack = _rBack.Contains(mp);
                _hover = overBack || _selOpt == 4 ? 3 : -1;
                bool guard = Time.unscaledTime - _settingsOpenedAt > 0.25f;
                bool back = guard && ((PAInput.MouseLeftDown() && overBack) ||
                                      PAInput.BackDown() ||
                                      (PAInput.ConfirmDown() && _selOpt == 4));
                if (back || PAInput.EscapeDown()) { _showSettings = false; PASettings.Save(); }
                return;   // menu buttons inactive behind the panel
            }

            int mh = _rStart.Contains(mp) ? 0 : (_rSettings.Contains(mp) ? 1 : (_rQuit.Contains(mp) ? 2 : -1));
            if (mh != -1) _hover = mh;
            int nav = PAInput.MenuNavY();
            if (nav != 0) _hover = _hover < 0 ? (nav > 0 ? 0 : 2) : Mathf.Clamp(_hover + nav, 0, 2);

            if (PAInput.MouseLeftDown() && mh != -1)
            {
                Activate(mh);
                return;
            }
            if (PAInput.ConfirmDown())
            {
                Activate(_hover >= 0 ? _hover : 0);
                return;
            }

            // The controller Start button is a direct shortcut. Keyboard Enter/Space is
            // handled above so it respects the currently focused menu item.
            if (PAInput.StartDown()) GameManager.Instance.StartGame();
        }

        void Activate(int item)
        {
            if (item == 0) GameManager.Instance.StartGame();
            else if (item == 1)
            {
                _showSettings = true;
                _selOpt = 0;
                _settingsOpenedAt = Time.unscaledTime;
                PASettings.Load();
            }
            else if (item == 2) GameManager.Instance.Quit();
        }

        void Layout()
        {
            float w = Screen.width, h = Screen.height;
            float bw = Mathf.Clamp(w * 0.26f, 220f, 440f);
            float bh = Mathf.Clamp(h * 0.082f, 46f, 88f);
            float cx = w * 0.5f - bw * 0.5f;
            float by = h * 0.58f;
            _rStart = new Rect(cx, by, bw, bh);
            _rSettings = new Rect(cx, by + bh * 1.25f, bw, bh);
            _rQuit = new Rect(cx, by + bh * 2.5f, bw, bh);

            _rPanel = new Rect(w * 0.5f - w * 0.20f, h * 0.20f, w * 0.40f, h * 0.56f);
            float bbw = Mathf.Clamp(w * 0.14f, 140f, 260f);
            _rBack = new Rect(w * 0.5f - bbw * 0.5f, _rPanel.yMax - h * 0.105f, bbw, Mathf.Clamp(h * 0.07f, 40f, 70f));
        }

        void OnGUI()
        {
            if (GameManager.Instance.State != GameState.Menu) return;
            BuildStyles();
            Layout();

            float w = Screen.width, h = Screen.height;
            float t = Time.unscaledTime - _startTime;

            float pos = t / Cycle;
            int cur = Mathf.FloorToInt(pos) % _bgs.Length;
            int nxt = (cur + 1) % _bgs.Length;
            float frac = pos - Mathf.Floor(pos);
            float fadeStart = (Cycle - Fade) / Cycle;
            float blend = frac > fadeStart ? (frac - fadeStart) / (Fade / Cycle) : 0f;

            DrawBackdrop(_bgs[cur], t, 1f, w, h);
            if (blend > 0f) DrawBackdrop(_bgs[nxt], t, blend, w, h);

            DrawGrass(t, cur, nxt, blend, w, h);

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, w, h), _vignette);
            GUI.color = Color.white;

            // Title block on a decorative banner
            float tbw = Mathf.Min(w * 0.72f, 920f);
            float tbh = tbw * 0.19f;
            Rect tb = new Rect(w * 0.5f - tbw * 0.5f, h * 0.06f, tbw, tbh);
            PAUI.DrawTitleBar(tb);
            Shadowed(tb, "CREK", _title);
            Shadowed(new Rect(0, tb.yMax + h * 0.005f, w, h * 0.06f), "A Post-Apocalypse Survival  —  Prologue", _sub);

            // Current scene name
            int shown = blend > 0.5f ? nxt : cur;
            GUI.Label(new Rect(w * 0.035f, h * 0.885f, w * 0.5f, h * 0.05f), "▮  " + SceneNames[shown], _tag);

            // Buttons
            Button(_rStart, "►   START", _hover == 0);
            Button(_rSettings, "⚙   SETTINGS", _hover == 1);
            Button(_rQuit, "✕   QUIT", _hover == 2);

            GUI.Label(new Rect(0, h * 0.94f, w, h * 0.05f),
                PAInput.GamepadPresent
                    ? "Press  Enter / Start  or  click START      •      WASD + Mouse  or  Gamepad"
                    : "Press  Enter  or  click START      •      WASD + Mouse to play", _foot);

            if (_showSettings)
            {
                GUI.color = new Color(0, 0, 0, 0.55f);
                GUI.DrawTexture(new Rect(0, 0, w, h), _px);
                GUI.color = Color.white;

                PAUI.DrawPanel(_rPanel);
                Shadowed(new Rect(_rPanel.x, _rPanel.y + h * 0.02f, _rPanel.width, h * 0.07f), "SETTINGS", _sub);

                float sx = _rPanel.x + _rPanel.width * 0.10f;
                float sw = _rPanel.width * 0.80f;
                float sy = _rPanel.y + h * 0.12f;
                float step = h * 0.075f;

                PASettings.Master = PAUI.OptSlider(sx, sy, sw, "MASTER VOLUME",
                    PASettings.Master, 11, Mathf.RoundToInt(PASettings.Master * 100f) + "%", _selOpt == 0);
                PASettings.Music = PAUI.OptSlider(sx, sy + step, sw, "MUSIC",
                    PASettings.Music, 12, Mathf.RoundToInt(PASettings.Music * 100f) + "%", _selOpt == 1);
                PASettings.Sfx = PAUI.OptSlider(sx, sy + step * 2f, sw, "SOUND  EFFECTS",
                    PASettings.Sfx, 13, Mathf.RoundToInt(PASettings.Sfx * 100f) + "%", _selOpt == 2);
                float sensT = Mathf.InverseLerp(0.2f, 2.5f, PASettings.Sensitivity);
                sensT = PAUI.OptSlider(sx, sy + step * 3f, sw, "LOOK  SENSITIVITY",
                    sensT, 14, PASettings.Sensitivity.ToString("0.0") + "x", _selOpt == 3);
                PASettings.Sensitivity = Mathf.Lerp(0.2f, 2.5f, sensT);

                if (AudioManager.I != null) AudioManager.I.ApplyVolumes();

                Button(_rBack, "◄   BACK", _hover == 3);
            }
        }

        void DrawBackdrop(Texture2D tex, float t, float alpha, float w, float h)
        {
            float scroll = t * 0.012f;                 // slow wind drift
            float sway = Mathf.Sin(t * 0.3f) * 0.01f;
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, w, h), tex, new Rect(scroll, sway, 1f, 1f));
            GUI.color = Color.white;
        }

        void DrawGrass(float t, int cur, int nxt, float blend, float w, float h)
        {
            float gh = h * 0.30f;
            float scroll = t * 0.05f + Mathf.Sin(t * 0.8f) * 0.015f;  // faster parallax + sway
            Color c = Color.Lerp(GrassTints[cur], GrassTints[nxt], blend);
            GUI.color = c;
            GUI.DrawTextureWithTexCoords(new Rect(0, h - gh, w, gh), _grass,
                new Rect(scroll, 0f, w / 260f, 1f));
            GUI.color = Color.white;
        }

        void Button(Rect r, string label, bool hover)
        {
            _btn.normal.textColor = hover ? new Color(1f, 0.86f, 0.6f) : new Color(0.80f, 0.72f, 0.55f);
            PAUI.DrawButton(r, label, hover, _btn);
        }

        void DrawBorder(Rect r, float t)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), _px);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), _px);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), _px);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), _px);
        }

        void Shadowed(Rect r, string s, GUIStyle st)
        {
            Color keep = st.normal.textColor;
            st.normal.textColor = new Color(0, 0, 0, 0.7f);
            GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), s, st);
            st.normal.textColor = keep;
            GUI.Label(r, s, st);
        }

        void BuildStyles()
        {
            if (!_stylesBuilt)
            {
                _title = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                _title.normal.textColor = new Color(0.86f, 0.78f, 0.58f);
                _sub = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal };
                _sub.normal.textColor = new Color(0.72f, 0.50f, 0.46f);
                _btn = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                _tag = new GUIStyle { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
                _tag.normal.textColor = new Color(0.85f, 0.78f, 0.6f);
                _foot = new GUIStyle { alignment = TextAnchor.MiddleCenter };
                _foot.normal.textColor = new Color(0.78f, 0.74f, 0.68f);
                _stylesBuilt = true;
            }
            float h = Screen.height;
            _title.fontSize = Mathf.RoundToInt(h * 0.075f);
            _sub.fontSize = Mathf.RoundToInt(h * 0.026f);
            _btn.fontSize = Mathf.RoundToInt(h * 0.030f);
            _tag.fontSize = Mathf.RoundToInt(h * 0.024f);
            _foot.fontSize = Mathf.RoundToInt(h * 0.020f);
        }
    }
}
