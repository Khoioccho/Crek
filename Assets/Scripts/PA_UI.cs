using UnityEngine;

namespace PostApoc
{
    // Dark-Souls-style UI skin built from procedural theme textures (deep crimson/purple,
    // cracked ruined edges). 9-sliced panels/buttons + a crimson health bar.
    public static class PAUI
    {
        static bool _init;
        public static Texture2D White;
        static Texture2D _panelTex, _btnTex, _btnOnTex, _hpFillTex;
        static GUIStyle _panel, _btnBg;

        public static void Ensure()
        {
            if (_init) return;
            _init = true;
            White = PAArt.Pixel(Color.white);
            _panelTex = PAArt.ThemePanel();
            _btnTex = PAArt.ThemeButton(false);
            _btnOnTex = PAArt.ThemeButton(true);
            _hpFillTex = PAArt.ThemeBarFill();

            _panel = new GUIStyle();
            _panel.normal.background = _panelTex;
            _panel.border = new RectOffset(40, 40, 40, 40);
        }

        static void Tint(Rect r, Color c) { var k = GUI.color; GUI.color = c; GUI.DrawTexture(r, White); GUI.color = k; }

        public static void DrawPanel(Rect rect) { Ensure(); GUI.Box(rect, GUIContent.none, _panel); }
        public static void DrawTitleBar(Rect rect) { Ensure(); GUI.Box(rect, GUIContent.none, _panel); }

        public static void DrawButton(Rect rect, string label, bool hover, GUIStyle textStyle)
        {
            Ensure();
            if (_btnBg == null) { _btnBg = new GUIStyle(); _btnBg.border = new RectOffset(26, 26, 26, 26); }
            _btnBg.normal.background = hover ? _btnOnTex : _btnTex;
            GUI.Box(rect, GUIContent.none, _btnBg);
            if (textStyle != null) GUI.Label(rect, label, textStyle);
        }

        // Shared immediate-mode settings slider (Input-System driven, usable from any screen).
        static GUIStyle _sLabel, _sValue;
        static int _sliderDrag = -1;

        public static float OptSlider(float x, float y, float w, string label, float v, int id, string valueText,
                                      bool focused = false)
        {
            Ensure();
            if (_sLabel == null)
            {
                _sLabel = new GUIStyle { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
                _sValue = new GUIStyle { alignment = TextAnchor.MiddleRight };
                _sValue.normal.textColor = new Color(0.72f, 0.60f, 0.48f);
            }
            float sh = Screen.height;
            _sLabel.fontSize = Mathf.RoundToInt(sh * 0.022f);
            _sValue.fontSize = Mathf.RoundToInt(sh * 0.020f);
            _sLabel.normal.textColor = focused ? new Color(1f, 0.88f, 0.62f) : new Color(0.85f, 0.74f, 0.55f);

            var labelR = new Rect(x, y, w * 0.55f, sh * 0.030f);
            GUI.Label(labelR, (focused ? "▸ " : "") + label, _sLabel);
            GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, labelR.height), valueText, _sValue);

            var track = new Rect(x, y + labelR.height + 2f, w, Mathf.Max(6f, sh * 0.012f));
            Vector2 mp = PAInput.MouseGui();
            var hit = new Rect(track.x - 6f, track.y - 8f, track.width + 12f, track.height + 16f);
            if (PAInput.MouseLeftDown() && hit.Contains(mp)) _sliderDrag = id;
            if (!PAInput.MouseHeld() && _sliderDrag == id) _sliderDrag = -1;
            if (_sliderDrag == id) v = Mathf.Clamp01((mp.x - track.x) / track.width);

            Tint(track, new Color(0.03f, 0.02f, 0.03f, 0.95f));
            var k = GUI.color;
            GUI.color = new Color(0.55f, 0.10f, 0.09f, 1f);
            GUI.DrawTexture(new Rect(track.x + 1f, track.y + 1f, (track.width - 2f) * v, track.height - 2f), White);
            GUI.color = new Color(0.85f, 0.70f, 0.45f, 1f);
            GUI.DrawTexture(new Rect(track.x + track.width * v - 2f, track.y - 3f, 4f, track.height + 6f), White);
            GUI.color = k;
            return v;
        }

        public static void DrawHealthBar(Rect rect, float frac)
        {
            Ensure();
            frac = Mathf.Clamp01(frac);
            Tint(rect, new Color(0.03f, 0.02f, 0.03f, 0.95f));           // dark backing

            float t = Mathf.Max(2f, rect.height * 0.13f);                // crimson frame lines
            var line = new Color(0.42f, 0.08f, 0.10f, 1f);
            Tint(new Rect(rect.x, rect.y, rect.width, t), line);
            Tint(new Rect(rect.x, rect.yMax - t, rect.width, t), line);
            Tint(new Rect(rect.x, rect.y, t, rect.height), line);
            Tint(new Rect(rect.xMax - t, rect.y, t, rect.height), line);

            float pad = t + rect.height * 0.10f;                          // crimson fill
            var inner = new Rect(rect.x + pad, rect.y + pad, (rect.width - pad * 2f) * frac, rect.height - pad * 2f);
            if (inner.width > 0f) GUI.DrawTexture(inner, _hpFillTex, ScaleMode.StretchToFill);
        }
    }
}
