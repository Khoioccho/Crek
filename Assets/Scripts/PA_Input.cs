using UnityEngine;
using UnityEngine.InputSystem;

namespace PostApoc
{
    // Thin wrapper over the new Input System (this project has legacy input disabled,
    // so UnityEngine.Input would throw). All gameplay reads input through here.
    public static class PAInput
    {
        public static Keyboard K => Keyboard.current;
        public static Mouse M => Mouse.current;
        public static Gamepad G => Gamepad.current;

        public static bool GamepadPresent => Gamepad.current != null;

        const float StickDeadzone = 0.18f;
        const float StickLookSpeed = 1700f;
        const float NavRepeatDelay = 0.40f;
        const float NavRepeatRate = 0.14f;

        static int _frame = -1;
        static bool _sprintLatch;
        static int _navX, _navY;
        static int _heldX, _heldY;
        static float _nextRepX, _nextRepY;

        static void Tick()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;

            var g = G;

            if (g != null && g.leftStickButton.wasPressedThisFrame) _sprintLatch = !_sprintLatch;
            if (g == null || g.leftStick.ReadValue().magnitude < StickDeadzone) _sprintLatch = false;

            _navX = StepRepeat(RawNav(g, true), ref _heldX, ref _nextRepX);
            _navY = StepRepeat(RawNav(g, false), ref _heldY, ref _nextRepY);
        }

        static int RawNav(Gamepad g, bool horizontal)
        {
            float v = 0f;
            if (g != null)
                v = horizontal
                    ? g.dpad.x.ReadValue() + g.leftStick.x.ReadValue()
                    : -(g.dpad.y.ReadValue() + g.leftStick.y.ReadValue());

            var k = K;
            if (k != null)
            {
                if (horizontal)
                {
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) v += 1f;
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) v -= 1f;
                }
                else
                {
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) v += 1f;
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) v -= 1f;
                }
            }
            return v > 0.5f ? 1 : (v < -0.5f ? -1 : 0);
        }

        static int StepRepeat(int raw, ref int held, ref float nextRep)
        {
            float t = Time.unscaledTime;
            if (raw == 0) { held = 0; return 0; }
            if (raw != held) { held = raw; nextRep = t + NavRepeatDelay; return raw; }
            if (t >= nextRep) { nextRep = t + NavRepeatRate; return raw; }
            return 0;
        }

        static Vector2 DeadzonedStick(Vector2 s)
        {
            float mag = s.magnitude;
            if (mag <= StickDeadzone) return Vector2.zero;
            return s / mag * ((mag - StickDeadzone) / (1f - StickDeadzone));
        }

        public static Vector2 Move()
        {
            float x = 0f, y = 0f;
            var k = K;
            if (k != null)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
            }
            var g = G;
            if (g != null)
            {
                Vector2 s = DeadzonedStick(g.leftStick.ReadValue());
                x += s.x; y += s.y;
            }
            var v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        public static Vector2 LookDelta()
        {
            var m = M;
            Vector2 d = m == null ? Vector2.zero : m.delta.ReadValue();
            var g = G;
            if (g != null)
            {
                Vector2 s = DeadzonedStick(g.rightStick.ReadValue());
                float t = s.magnitude;
                if (t > 0f)
                {
                    Vector2 stick = s / t * (t * t * StickLookSpeed * Time.deltaTime);
                    d.x += stick.x;
                    d.y += stick.y * 0.8f;
                }
            }
            return d;
        }

        public static bool Run()
        {
            Tick();
            var k = K;
            return (k != null && k.leftShiftKey.isPressed) || _sprintLatch;
        }

        public static bool JumpDown()
        {
            var k = K; if (k != null && k.spaceKey.wasPressedThisFrame) return true;
            var g = G; return g != null && g.buttonSouth.wasPressedThisFrame;
        }

        public static bool TogglePerspectiveDown()
        {
            var k = K; if (k != null && k.vKey.wasPressedThisFrame) return true;
            var g = G; return g != null && g.selectButton.wasPressedThisFrame;
        }

        public static bool AttackDown()
        {
            var m = M; if (m != null && m.leftButton.wasPressedThisFrame) return true;
            var k = K; if (k != null && k.fKey.wasPressedThisFrame) return true;
            var g = G;
            return g != null && (g.rightShoulder.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame);
        }

        public static bool HeavyDown()
        {
            var m = M; if (m != null && m.rightButton.wasPressedThisFrame) return true;
            var g = G; return g != null && g.rightTrigger.wasPressedThisFrame;
        }

        public static bool RollDown()
        {
            var k = K; if (k != null && k.cKey.wasPressedThisFrame) return true;
            var g = G; return g != null && g.buttonEast.wasPressedThisFrame;
        }

        public static bool LockOnDown()
        {
            var k = K;
            if (k != null && (k.tabKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame)) return true;
            var m = M; if (m != null && m.middleButton.wasPressedThisFrame) return true;
            var g = G; return g != null && g.rightStickButton.wasPressedThisFrame;
        }

        public static float ScrollY()
        {
            var m = M;
            float v = m == null ? 0f : m.scroll.ReadValue().y;
            var g = G;
            if (g != null)
            {
                if (g.dpad.up.wasPressedThisFrame) v += 1f;
                if (g.dpad.down.wasPressedThisFrame) v -= 1f;
            }
            return v;
        }

        public static bool InteractDown()
        {
            var k = K; if (k != null && k.eKey.wasPressedThisFrame) return true;
            var g = G; return g != null && g.buttonNorth.wasPressedThisFrame;
        }

        public static bool EscapeDown()
        {
            var k = K; if (k != null && k.escapeKey.wasPressedThisFrame) return true;
            var g = G; return g != null && g.startButton.wasPressedThisFrame;
        }

        public static bool MouseLeftDown() { var m = M; return m != null && m.leftButton.wasPressedThisFrame; }
        public static bool MouseHeld() { var m = M; return m != null && m.leftButton.isPressed; }

        public static bool StartDown()
        {
            var k = K;
            if (k != null && (k.enterKey.wasPressedThisFrame ||
                              k.numpadEnterKey.wasPressedThisFrame ||
                              k.spaceKey.wasPressedThisFrame)) return true;
            var g = G; return g != null && g.startButton.wasPressedThisFrame;
        }

        public static bool ConfirmDown() => MenuSubmitDown();

        // Keyboard/gamepad confirmation for modal menus such as the death screen.
        public static bool MenuSubmitDown()
        {
            var k = K;
            if (k != null && (k.enterKey.wasPressedThisFrame ||
                              k.numpadEnterKey.wasPressedThisFrame ||
                              k.spaceKey.wasPressedThisFrame)) return true;
            var g = G; return g != null && g.buttonSouth.wasPressedThisFrame;
        }

        public static bool BackDown()
        {
            var k = K;
            if (k != null && k.escapeKey.wasPressedThisFrame) return true;
            var g = G;
            return g != null && g.buttonEast.wasPressedThisFrame;
        }

        public static int MenuNavX() { Tick(); return _navX; }
        public static int MenuNavY() { Tick(); return _navY; }

        // Used to advance dialogue / dismiss banners.
        public static bool AdvanceDown()
        {
            var k = K;
            bool kb = k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame ||
                                    k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
            var m = M;
            bool mc = m != null && m.leftButton.wasPressedThisFrame;
            var g = G;
            bool gp = g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame);
            return kb || mc || gp;
        }

        // Mouse position in GUI space (origin top-left), for IMGUI hit-testing.
        public static Vector2 MouseGui()
        {
            var m = M;
            if (m == null) return new Vector2(-1f, -1f);
            Vector2 p = m.position.ReadValue();
            return new Vector2(p.x, Screen.height - p.y);
        }
    }
}
