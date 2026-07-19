using System.Collections;
using UnityEngine;

namespace PostApoc
{
    public enum GameState { Menu, Playing }

    // Owns the shared camera/sun, the menu, the HUD, and orchestrates starting the game.
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        // Lazily re-finds the manager so a domain reload during Play mode (which nulls
        // statics but keeps the live objects) can't leave other scripts dereferencing null.
        static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<GameManager>();
                return _instance;
            }
            private set { _instance = value; }
        }

        public GameState State = GameState.Menu;
        public bool Paused;

        public Camera Cam;
        public Light Sun;
        public HUD Hud;
        public MainMenu Menu;
        public WorldBuilder World;
        public PlayerController Player;
        public TutorialManager Tutorial;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // The 2D template scene ships a "Global Light 2D" that does nothing useful in 3D.
            var leftover = GameObject.Find("Global Light 2D");
            if (leftover != null) Destroy(leftover);

            SetupCamera();
            SetupSun();

            gameObject.AddComponent<AudioManager>();
            Hud = gameObject.AddComponent<HUD>();
            Menu = gameObject.AddComponent<MainMenu>();

            EnterMenu();
        }

        void SetupCamera()
        {
            Cam = Camera.main;
            if (Cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                Cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            Cam.orthographic = false;
            Cam.fieldOfView = 62f;
            Cam.nearClipPlane = 0.05f;
            Cam.farClipPlane = 600f;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            Cam.transform.SetParent(null);
            Cam.transform.SetPositionAndRotation(new Vector3(0f, 2f, -10f), Quaternion.identity);
        }

        void SetupSun()
        {
            Light found = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (l.type == LightType.Directional) { found = l; break; }
            if (found == null)
            {
                var go = new GameObject("Sun");
                found = go.AddComponent<Light>();
                found.type = LightType.Directional;
            }
            Sun = found;
            Sun.intensity = 0f; // dark until the world is built
        }

        public void EnterMenu()
        {
            State = GameState.Menu;
            Paused = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Menu != null) Menu.enabled = true;
            if (Hud != null) Hud.ResetAll();
        }

        public void StartGame()
        {
            if (State == GameState.Playing) return;
            State = GameState.Playing;
            if (Menu != null) Menu.enabled = false;

            var worldGo = new GameObject("World");
            World = worldGo.AddComponent<WorldBuilder>();
            World.Build(Cam, Sun);

            Player = World.SpawnPlayer(Cam);

            var t = new GameObject("Tutorial");
            Tutorial = t.AddComponent<TutorialManager>();
            Tutorial.Begin(World, Player, Hud);
        }

        void Update()
        {
            if (State == GameState.Playing && PAInput.EscapeDown())
                TogglePause();
        }

        void TogglePause()
        {
            Paused = !Paused;
            Cursor.lockState = Paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Paused;
            if (!Paused) PASettings.Save();   // persist any settings changed in the pause menu
        }

        // Destroy the in-progress game (world, player, tutorial) and clear the combat registry.
        void Teardown()
        {
            if (Tutorial != null) { Destroy(Tutorial.gameObject); Tutorial = null; }
            if (Player != null) { Destroy(Player.gameObject); Player = null; }
            if (World != null) { Destroy(World.gameObject); World = null; }
            Combatant.All.Clear();
        }

        // Tear down and return to the main menu.
        public void ReturnToMenu()
        {
            Teardown();
            if (Cam != null) Cam.transform.SetPositionAndRotation(new Vector3(0f, 2f, -10f), Quaternion.identity);
            EnterMenu();
        }

        // Full restart: tear down and replay the prologue from waking up in the house.
        public void RestartGame() { StartCoroutine(RestartRoutine()); }

        IEnumerator RestartRoutine()
        {
            if (Hud != null) { Hud.ResetAll(); Hud.SetFade(1f); }   // clear stale HUD, cut to black
            Teardown();
            State = GameState.Menu;   // let StartGame's guard pass
            yield return null;        // wait a frame for the old objects to finish destroying
            StartGame();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
