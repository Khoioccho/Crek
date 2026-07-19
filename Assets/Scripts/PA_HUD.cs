using System;
using System.Collections;
using UnityEngine;

namespace PostApoc
{
    // In-game 2D overlay: fade, objective, toasts, interaction prompt, off-screen waypoint,
    // dialogue box, end banner, crosshair and a styled health bar (gui_parts sprites).
    public class HUD : MonoBehaviour
    {
        public float Fade;
        string _objective = "";
        string _toast = ""; float _toastUntil = -1f;
        string _prompt = "";
        bool _hasWaypoint; Vector3 _waypoint; string _waypointLabel = "";

        string[] _dlg; int _dlgIdx; bool _dlgActive; Action _dlgDone; float _dlgOpened;
        bool _banner; string _bannerT = "", _bannerS = "";

        string _bossName; bool _bossOn; float _bossMax;
        int _drag = -1;   // active settings slider
        int _selOpt;
        string _ds3Text = ""; Color _ds3Color; float _ds3Band, _ds3TextA;
        bool _deathMenu; int _deathChoice = -1;

        Texture2D _px;
        GUIStyle _obj, _toastSt, _promptSt, _wp, _dlgSt, _hint, _bigT, _bigS, _hpNum, _optLabel, _optValue;
        bool _built;

        public bool DialogueActive => _dlgActive;

        void Awake() { _px = PAArt.Pixel(Color.white); }

        public void ResetAll()
        {
            Fade = 0f; _objective = ""; _toast = ""; _prompt = "";
            _hasWaypoint = false; _dlgActive = false; _banner = false;
            _bossOn = false; _deathMenu = false; _deathChoice = -1;
        }

        public void SetFade(float f) { Fade = Mathf.Clamp01(f); }

        public IEnumerator FadeTo(float target, float dur)
        {
            float s = Fade, e = Mathf.Clamp01(target), t = 0f;
            while (t < dur) { t += Time.deltaTime; Fade = Mathf.Lerp(s, e, t / dur); yield return null; }
            Fade = e;
        }

        public void SetObjective(string s) { _objective = s; }
        public void ClearObjective() { _objective = ""; }
        public void ShowToast(string s, float dur) { _toast = s; _toastUntil = Time.time + dur; }
        public void SetPrompt(string s) { _prompt = s; }
        public void ClearPrompt() { _prompt = ""; }
        public void SetWaypoint(Vector3 w, string label) { _hasWaypoint = true; _waypoint = w; _waypointLabel = label; }
        public void ClearWaypoint() { _hasWaypoint = false; }
        public void ShowBanner(string t, string s)
        {
            _banner = true; _bannerT = t; _bannerS = s;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;   // so the menu button is clickable
        }
        // Locks in the wave's combined max HP at call time (call AFTER spawning the wave),
        // so kills drain the bar permanently instead of rescaling it.
        public void SetBossBar(string name)
        {
            _bossName = name; _bossOn = true;
            _bossMax = 0f;
            foreach (var c in Combatant.All)
                if (c != null && !c.IsDead && c.faction == Faction.Enemy) _bossMax += c.maxHealth;
        }
        public void ClearBossBar() { _bossOn = false; }

        // Death screen with a RETRY / QUIT menu. The tutorial polls DeathChoice
        // (0 = retry, 1 = quit) and then calls HideDeathMenu().
        public bool DeathMenuActive => _deathMenu;
        public int DeathChoice => _deathChoice;
        public void ShowDeathMenu()
        {
            _deathMenu = true; _deathChoice = -1;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public void HideDeathMenu() { _deathMenu = false; _deathChoice = -1; }

        // Dark-Souls-style center band ("YOU DIED" / "VICTORY ACHIEVED").
        public IEnumerator BigBanner(string text, Color color, float hold)
        {
            _ds3Text = text; _ds3Color = color; _ds3Band = 0f; _ds3TextA = 0f;
            float t = 0f;
            while (t < 1.1f) { t += Time.deltaTime; _ds3Band = Mathf.Clamp01(t / 1.1f); _ds3TextA = Mathf.Clamp01((t - 0.25f) / 0.9f); yield return null; }
            yield return new WaitForSeconds(hold);
            t = 0f;
            while (t < 0.8f) { t += Time.deltaTime; float k = 1f - t / 0.8f; _ds3Band = k; _ds3TextA = k; yield return null; }
            _ds3Text = ""; _ds3Band = 0f; _ds3TextA = 0f;
        }

        public void ShowDialogue(string[] lines, Action onDone)
        {
            _dlg = lines; _dlgIdx = 0; _dlgActive = true; _dlgDone = onDone; _dlgOpened = Time.time;
            if (GameManager.Instance.Player != null) GameManager.Instance.Player.controlEnabled = false;
            PlayVillagerVoice();
        }

        static void PlayVillagerVoice()
        {
            var gm = GameManager.Instance;
            Vector3 pos = gm != null && gm.World != null && gm.World.Villager != null
                ? gm.World.Villager.transform.position + Vector3.up * 1.5f
                : (gm != null && gm.Cam != null ? gm.Cam.transform.position : Vector3.zero);
            AudioManager.Sfx(AudioManager.VillagerVoice, pos, 0.9f);
        }

        void Update()
        {
            if (_toast != "" && Time.time > _toastUntil) _toast = "";

            var gmP = GameManager.Instance;
            if (gmP != null && gmP.Paused && gmP.State == GameState.Playing)
            {
                int ny = PAInput.MenuNavY();
                if (ny != 0) _selOpt = Mathf.Clamp(_selOpt + ny, 0, 3);
                int nx = PAInput.MenuNavX();
                if (nx != 0) PASettings.Adjust(_selOpt, nx);
            }

            if (_dlgActive && Time.time - _dlgOpened > 0.18f && PAInput.AdvanceDown())
            {
                _dlgIdx++;
                if (_dlg == null || _dlgIdx >= _dlg.Length)
                {
                    _dlgActive = false;
                    var cb = _dlgDone; _dlgDone = null;
                    if (GameManager.Instance.Player != null) GameManager.Instance.Player.controlEnabled = true;
                    cb?.Invoke();
                }
                else { _dlgOpened = Time.time; PlayVillagerVoice(); }
            }
        }

        void Dim(Rect r, float a)
        {
            GUI.color = new Color(0, 0, 0, a);
            GUI.DrawTexture(r, _px);
            GUI.color = Color.white;
        }

        // Immediate-mode slider driven by the Input System (IMGUI-event independent).
        // Returns the new 0..1 value.
        float OptSlider(float x, float y, float w, string label, float v, int id, string valueText,
                        bool focused = false)
        {
            var labelR = new Rect(x, y, w * 0.55f, Screen.height * 0.030f);
            _optLabel.normal.textColor = focused ? new Color(1f, 0.88f, 0.62f) : new Color(0.85f, 0.74f, 0.55f);
            GUI.Label(labelR, (focused ? "▸ " : "") + label, _optLabel);
            GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, labelR.height), valueText, _optValue);

            var track = new Rect(x, y + labelR.height + 2f, w, Mathf.Max(6f, Screen.height * 0.012f));
            Vector2 mp = PAInput.MouseGui();
            var hit = new Rect(track.x - 6f, track.y - 8f, track.width + 12f, track.height + 16f);
            if (PAInput.MouseLeftDown() && hit.Contains(mp)) _drag = id;
            if (!PAInput.MouseHeld() && _drag == id) _drag = -1;
            if (_drag == id) v = Mathf.Clamp01((mp.x - track.x) / track.width);

            GUI.color = new Color(0.03f, 0.02f, 0.03f, 0.95f);
            GUI.DrawTexture(track, _px);
            GUI.color = new Color(0.55f, 0.10f, 0.09f, 1f);
            GUI.DrawTexture(new Rect(track.x + 1f, track.y + 1f, (track.width - 2f) * v, track.height - 2f), _px);
            GUI.color = new Color(0.85f, 0.70f, 0.45f, 1f);
            float kx = track.x + track.width * v;
            GUI.DrawTexture(new Rect(kx - 2f, track.y - 3f, 4f, track.height + 6f), _px);
            GUI.color = Color.white;
            return v;
        }

        void OnGUI()
        {
            var gm = GameManager.Instance;
            bool playing = gm != null && gm.State == GameState.Playing;
            if (!playing && Fade <= 0f && !_banner) return;
            BuildStyles();

            float w = Screen.width, h = Screen.height;
            bool thirdPerson = gm != null && gm.Player != null && gm.Player.ThirdPerson;

            if (playing && !_dlgActive && !_banner && !gm.Paused && !thirdPerson)
            {
                float cs = Mathf.Max(3f, h * 0.004f);
                GUI.color = new Color(1, 1, 1, 0.6f);
                GUI.DrawTexture(new Rect(w / 2 - cs / 2, h / 2 - cs / 2, cs, cs), _px);
                GUI.color = Color.white;
            }

            if (playing && !_banner && !gm.Paused) DrawEnemyBars(w, h);

            if (_hasWaypoint && playing && !_banner) DrawWaypoint(w, h);

            // health bar (bottom-left)
            if (playing && gm.Player != null && gm.Player.Combat != null && !_banner)
            {
                var pl = gm.Player;
                var cb = pl.Combat;

                // DS3 layout: HP + stamina top-left
                var hr = new Rect(w * 0.025f, h * 0.035f, w * 0.26f, h * 0.034f);
                PAUI.DrawHealthBar(hr, cb.health / cb.maxHealth);
                var sr = new Rect(hr.x, hr.yMax + h * 0.008f, hr.width * 0.78f, h * 0.026f);
                GUI.color = new Color(0.03f, 0.03f, 0.02f, 0.95f);
                GUI.DrawTexture(sr, _px);
                GUI.color = new Color(0.28f, 0.45f, 0.16f, 1f);
                float sfrac = pl.maxStamina > 0f ? Mathf.Clamp01(pl.stamina / pl.maxStamina) : 0f;
                GUI.DrawTexture(new Rect(sr.x + 2f, sr.y + 2f, (sr.width - 4f) * sfrac, sr.height - 4f), _px);
                GUI.color = Color.white;

                // boss-style wave bar bottom-center
                if (_bossOn && _bossMax > 0f)
                {
                    // numerator = living enemies' HP; denominator = the wave's total, frozen
                    // at spawn — dead enemies stay subtracted instead of rescaling the bar.
                    float cur = 0f;
                    foreach (var c in Combatant.All)
                        if (c != null && !c.IsDead && c.faction == Faction.Enemy) cur += c.health;

                    var nameR = new Rect(w * 0.22f, h * 0.835f, w * 0.56f, h * 0.035f);
                    GUI.Label(nameR, _bossName, _obj);
                    var bb = new Rect(w * 0.22f, h * 0.875f, w * 0.56f, h * 0.016f);
                    GUI.color = new Color(0.02f, 0.015f, 0.02f, 0.9f);
                    GUI.DrawTexture(bb, _px);
                    GUI.color = new Color(0.55f, 0.08f, 0.07f, 1f);
                    GUI.DrawTexture(new Rect(bb.x + 1.5f, bb.y + 1.5f, (bb.width - 3f) * Mathf.Clamp01(cur / _bossMax), bb.height - 3f), _px);
                    GUI.color = Color.white;
                }

                // lock-on reticle
                var lt = pl.LockTarget;
                if (lt != null && gm.Cam != null)
                {
                    Vector3 sp = gm.Cam.WorldToScreenPoint(lt.transform.position + Vector3.up * 1.1f);
                    if (sp.z > 0f)
                    {
                        var rs = Mathf.Max(10f, h * 0.016f);
                        GUI.Label(new Rect(sp.x - rs, (h - sp.y) - rs, rs * 2f, rs * 2f), "◆", _wp);
                    }
                }
            }

            if (_objective != "")
            {
                var r = new Rect(w * 0.5f - w * 0.24f, h * 0.035f, w * 0.48f, h * 0.06f);
                Dim(r, 0.55f);
                GUI.Label(r, "◈   " + _objective, _obj);
            }

            if (_toast != "")
            {
                var r = new Rect(w * 0.5f - w * 0.34f, h * 0.13f, w * 0.68f, h * 0.08f);
                Dim(r, 0.5f);
                GUI.Label(r, _toast, _toastSt);
            }

            if (_prompt != "" && !_dlgActive)
                GUI.Label(new Rect(0, h * 0.60f, w, h * 0.06f), _prompt, _promptSt);

            if (_dlgActive && _dlg != null && _dlgIdx < _dlg.Length) DrawDialogue(w, h);

            if (gm != null && gm.Paused && playing && !_banner)
            {
                Dim(new Rect(0, 0, w, h), 0.5f);
                PASettings.Load();
                var pr = new Rect(w * 0.5f - w * 0.20f, h * 0.20f, w * 0.40f, h * 0.56f);
                PAUI.DrawPanel(pr);
                GUI.Label(new Rect(pr.x, pr.y + h * 0.025f, pr.width, h * 0.07f), "SETTINGS", _bigT);

                float sx = pr.x + pr.width * 0.10f;
                float sw = pr.width * 0.80f;
                float sy = pr.y + h * 0.13f;
                float step = h * 0.075f;

                PASettings.Master = OptSlider(sx, sy, sw, "MASTER VOLUME",
                    PASettings.Master, 1, Mathf.RoundToInt(PASettings.Master * 100f) + "%", _selOpt == 0);
                PASettings.Music = OptSlider(sx, sy + step, sw, "MUSIC",
                    PASettings.Music, 2, Mathf.RoundToInt(PASettings.Music * 100f) + "%", _selOpt == 1);
                PASettings.Sfx = OptSlider(sx, sy + step * 2f, sw, "SOUND  EFFECTS",
                    PASettings.Sfx, 3, Mathf.RoundToInt(PASettings.Sfx * 100f) + "%", _selOpt == 2);

                float sensT = Mathf.InverseLerp(0.2f, 2.5f, PASettings.Sensitivity);
                sensT = OptSlider(sx, sy + step * 3f, sw, "LOOK  SENSITIVITY",
                    sensT, 4, PASettings.Sensitivity.ToString("0.0") + "x", _selOpt == 3);
                PASettings.Sensitivity = Mathf.Lerp(0.2f, 2.5f, sensT);

                if (AudioManager.I != null) AudioManager.I.ApplyVolumes();

                GUI.Label(new Rect(pr.x, pr.yMax - h * 0.055f, pr.width, h * 0.04f),
                    PAInput.GamepadPresent ? "Esc / Start — resume" : "Esc — resume", _toastSt);
            }
            else _drag = -1;

            if (_banner) DrawBanner(w, h);

            // Dark-Souls center band (YOU DIED / VICTORY ACHIEVED)
            if (_ds3Band > 0.01f && _ds3Text != "")
            {
                float bandH = h * 0.20f;
                var band = new Rect(0, h * 0.5f - bandH * 0.5f, w, bandH);
                Dim(band, 0.82f * _ds3Band);
                GUI.color = new Color(0.30f, 0.06f, 0.05f, 0.5f * _ds3Band);
                GUI.DrawTexture(new Rect(band.x, band.y, w, 2f), _px);
                GUI.DrawTexture(new Rect(band.x, band.yMax - 2f, w, 2f), _px);
                GUI.color = Color.white;
                var c = _ds3Color; c.a = _ds3TextA;
                _bigT.normal.textColor = c;
                int keep = _bigT.fontSize;
                _bigT.fontSize = Mathf.RoundToInt(h * 0.085f);
                GUI.Label(band, _ds3Text, _bigT);
                _bigT.fontSize = keep;
                _bigT.normal.textColor = new Color(0.90f, 0.72f, 0.48f);
            }

            if (_deathMenu) DrawDeathMenu(w, h);

            if (Fade > 0f) Dim(new Rect(0, 0, w, h), Fade);
        }

        // A clickable themed button (Input-System driven, IMGUI-event independent).
        bool Button(Rect r, string label, bool enabled)
        {
            Vector2 mp = PAInput.MouseGui();
            bool hover = r.Contains(mp);
            PAUI.DrawButton(r, label, hover, _obj);
            return enabled && hover && PAInput.MouseLeftDown();
        }

        void DrawDeathMenu(float w, float h)
        {
            Dim(new Rect(0, 0, w, h), 0.80f);

            // giant crimson "YOU DIED"
            var keepC = _bigT.normal.textColor; int keepS = _bigT.fontSize;
            _bigT.normal.textColor = new Color(0.66f, 0.08f, 0.06f);
            _bigT.fontSize = Mathf.RoundToInt(h * 0.10f);
            GUI.Label(new Rect(0, h * 0.24f, w, h * 0.16f), "YOU DIED", _bigT);
            _bigT.fontSize = keepS; _bigT.normal.textColor = keepC;

            GUI.Label(new Rect(0, h * 0.42f, w, h * 0.05f), "The village still needs you.", _toastSt);

            float bw = Mathf.Clamp(w * 0.24f, 240f, 460f), bh = Mathf.Max(48f, h * 0.082f);
            float cx = w * 0.5f - bw * 0.5f;
            var retryR = new Rect(cx, h * 0.52f, bw, bh);
            var quitR = new Rect(cx, retryR.yMax + h * 0.022f, bw, bh);

            bool en = _deathChoice < 0;
            if (Button(retryR, "RETRY", en)) _deathChoice = 0;
            if (Button(quitR, "QUIT TO MENU", en)) _deathChoice = 1;
        }

        void DrawDialogue(float w, float h)
        {
            var r = new Rect(w * 0.12f, h * 0.68f, w * 0.76f, h * 0.24f);
            PAUI.DrawPanel(r);
            var inner = new Rect(r.x + w * 0.045f, r.y + h * 0.045f, r.width - w * 0.09f, r.height - h * 0.10f);
            GUI.Label(inner, _dlg[_dlgIdx], _dlgSt);
            GUI.Label(new Rect(r.x, r.yMax - h * 0.06f, r.width - w * 0.05f, h * 0.04f), "[E] / Click  ▼", _hint);
        }

        void DrawBanner(float w, float h)
        {
            Dim(new Rect(0, 0, w, h), 0.6f);
            var r = new Rect(w * 0.5f - w * 0.30f, h * 0.30f, w * 0.60f, h * 0.36f);
            PAUI.DrawPanel(r);
            GUI.Label(new Rect(0, h * 0.35f, w, h * 0.12f), _bannerT, _bigT);
            GUI.Label(new Rect(w * 0.5f - w * 0.27f, h * 0.47f, w * 0.54f, h * 0.08f), _bannerS, _bigS);

            float bw = Mathf.Clamp(w * 0.22f, 220f, 400f), bh = Mathf.Max(46f, h * 0.075f);
            var btn = new Rect(w * 0.5f - bw * 0.5f, h * 0.565f, bw, bh);
            if (Button(btn, "EXIT TO MENU", true))
            {
                _banner = false;
                GameManager.Instance?.ReturnToMenu();
            }
        }

        void DrawEnemyBars(float w, float h)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Cam == null) return;
            var cam = gm.Cam;
            Vector3 camPos = cam.transform.position;
            float bw = Mathf.Clamp(w * 0.05f, 48f, 90f);
            float bh = Mathf.Max(5f, h * 0.008f);
            var playerCb = gm.Player != null ? gm.Player.Combat : null;

            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead) continue;

                bool enemy = c.faction == Faction.Enemy;
                // villager (friendly) bars only appear once the wave attack is on, and never
                // over the player himself (he already has the corner HP bar).
                if (!enemy && (!_bossOn || c == playerCb)) continue;

                if ((c.transform.position - camPos).sqrMagnitude > 45f * 45f) continue;
                Vector3 sp = cam.WorldToScreenPoint(c.transform.position + Vector3.up * 2.0f);
                if (sp.z <= 0f) continue;

                float x = sp.x - bw * 0.5f;
                float y = (h - sp.y);
                if (x + bw < 0f || x > w) continue;

                float frac = c.maxHealth > 0f ? Mathf.Clamp01(c.health / c.maxHealth) : 0f;
                Color fill = enemy ? new Color(0.85f, 0.16f, 0.13f, 1f)    // crimson enemies
                                   : new Color(0.32f, 0.72f, 0.28f, 1f);   // green allies
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(new Rect(x - 1f, y - 1f, bw + 2f, bh + 2f), _px);
                GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
                GUI.DrawTexture(new Rect(x, y, bw, bh), _px);
                GUI.color = fill;
                GUI.DrawTexture(new Rect(x, y, bw * frac, bh), _px);
                GUI.color = Color.white;
            }
        }

        void DrawWaypoint(float w, float h)
        {
            var gm = GameManager.Instance;
            Vector3 sp = gm.Cam.WorldToScreenPoint(_waypoint + Vector3.up * 2f);
            float dist = gm.Player != null ? Vector3.Distance(gm.Player.transform.position, _waypoint) : 0f;
            string label = "◇  " + _waypointLabel + "   " + Mathf.RoundToInt(dist) + "m";

            float gx = sp.x, gy = Screen.height - sp.y;
            if (sp.z < 0f) { gx = Screen.width - gx; gy = Screen.height * 0.88f; }
            float m = 70f;
            gx = Mathf.Clamp(gx, m, w - m);
            gy = Mathf.Clamp(gy, m, h - m);

            var r = new Rect(gx - 95, gy - 18, 190, 36);
            Dim(r, 0.5f);
            GUI.Label(r, label, _wp);
        }

        void BuildStyles()
        {
            if (!_built)
            {
                _obj = C(new Color(0.88f, 0.66f, 0.38f), FontStyle.Bold);
                _toastSt = C(new Color(0.92f, 0.90f, 0.85f), FontStyle.Normal); _toastSt.wordWrap = true;
                _promptSt = C(new Color(0.92f, 0.80f, 0.55f), FontStyle.Bold);
                _wp = C(new Color(0.85f, 0.74f, 0.55f), FontStyle.Bold);
                _dlgSt = new GUIStyle { alignment = TextAnchor.UpperLeft, wordWrap = true };
                _dlgSt.normal.textColor = new Color(0.96f, 0.93f, 0.86f);
                _hint = new GUIStyle { alignment = TextAnchor.LowerRight };
                _hint.normal.textColor = new Color(0.8f, 0.75f, 0.6f);
                _bigT = C(new Color(0.90f, 0.72f, 0.48f), FontStyle.Bold);
                _bigS = C(new Color(0.9f, 0.86f, 0.8f), FontStyle.Normal); _bigS.wordWrap = true;
                _hpNum = C(new Color(1f, 0.95f, 0.9f), FontStyle.Bold);
                _optLabel = new GUIStyle { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
                _optLabel.normal.textColor = new Color(0.85f, 0.74f, 0.55f);
                _optValue = new GUIStyle { alignment = TextAnchor.MiddleRight };
                _optValue.normal.textColor = new Color(0.72f, 0.60f, 0.48f);
                _built = true;
            }
            float h = Screen.height;
            _obj.fontSize = Mathf.RoundToInt(h * 0.026f);
            _toastSt.fontSize = Mathf.RoundToInt(h * 0.024f);
            _promptSt.fontSize = Mathf.RoundToInt(h * 0.028f);
            _wp.fontSize = Mathf.RoundToInt(h * 0.020f);
            _dlgSt.fontSize = Mathf.RoundToInt(h * 0.028f);
            _hint.fontSize = Mathf.RoundToInt(h * 0.018f);
            _bigT.fontSize = Mathf.RoundToInt(h * 0.058f);
            _bigS.fontSize = Mathf.RoundToInt(h * 0.026f);
            _hpNum.fontSize = Mathf.RoundToInt(h * 0.020f);
            _optLabel.fontSize = Mathf.RoundToInt(h * 0.022f);
            _optValue.fontSize = Mathf.RoundToInt(h * 0.020f);
        }

        static GUIStyle C(Color c, FontStyle fs)
        {
            var s = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = fs };
            s.normal.textColor = c;
            return s;
        }
    }
}
