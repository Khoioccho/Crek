using UnityEngine;
using UnityEngine.Rendering;

namespace PostApoc
{
    // Builds the entire 3D tutorial world out of primitives + procedural materials:
    // the ruined house you wake in, a path north, and a village with the first NPC.
    public class WorldBuilder : MonoBehaviour
    {
        public Vector3 HouseCenter = Vector3.zero;
        public Vector3 PlayerSpawn = new Vector3(-1.6f, 0f, -2.0f);
        public float PlayerStartYaw = 0f;                 // face +Z (toward the door)
        public Vector3 VillageCenter = new Vector3(0f, 0f, 62f);
        public NPC Villager;

        Material _ground, _wall, _wood, _roof, _floor, _sheet, _pillow, _rock,
                 _trunk, _path, _hutWall, _hutRoof, _coat, _skin, _hat,
                 _goblin, _goblinDark, _fire, _eye, _marker;

        // ------------------------------------------------------------------

        public void Build(Camera cam, Light sun)
        {
            Debug.Log($"[PostApoc] Models present — Player:{PAModels.Has("Player")}  " +
                      $"Skeleton:{PAModels.Has("Skeleton")}  Villagers:{PAModels.Has("Villager")}  " +
                      $"Rocks:{PAModels.Has("Props/SM_Rock_1")}  Village:{PAModels.Has("Village/rpgpp_lt_building_01")}");
            SetupMaterials();
            SetupEnvironment(cam, sun);
            BuildGround();
            BuildHouse();
            BuildPath();
            BuildVillage();
            BuildDecorations();
        }

        void SetupMaterials()
        {
            _ground = PAArt.MatTex(PAArt.Dirt(), new Color(0.85f, 0.82f, 0.78f), new Vector2(80, 80));
            _wall = PAArt.Mat(new Color(0.55f, 0.50f, 0.42f));
            _wood = PAArt.Mat(new Color(0.27f, 0.19f, 0.12f));
            _roof = PAArt.Mat(new Color(0.36f, 0.18f, 0.12f));
            _floor = PAArt.Mat(new Color(0.34f, 0.30f, 0.26f));
            _sheet = PAArt.Mat(new Color(0.50f, 0.46f, 0.42f));
            _pillow = PAArt.Mat(new Color(0.72f, 0.68f, 0.62f));
            _rock = PAArt.Mat(new Color(0.40f, 0.40f, 0.42f));
            _trunk = PAArt.Mat(new Color(0.20f, 0.15f, 0.11f));
            _path = PAArt.Mat(new Color(0.26f, 0.21f, 0.15f));
            _hutWall = PAArt.Mat(new Color(0.50f, 0.42f, 0.32f));
            _hutRoof = PAArt.Mat(new Color(0.32f, 0.16f, 0.10f));
            _coat = PAArt.Mat(new Color(0.36f, 0.30f, 0.22f));
            _skin = PAArt.Mat(new Color(0.78f, 0.60f, 0.48f));
            _hat = PAArt.Mat(new Color(0.30f, 0.24f, 0.16f));
            _goblin = PAArt.Mat(new Color(0.32f, 0.45f, 0.22f));
            _goblinDark = PAArt.Mat(new Color(0.22f, 0.32f, 0.16f));
            _fire = PAArt.MatEmissive(new Color(1f, 0.5f, 0.15f), 2.2f);
            _eye = PAArt.MatEmissive(new Color(1f, 0.15f, 0.05f), 2.5f);
            _marker = PAArt.MatEmissive(new Color(1f, 0.82f, 0.32f), 2.0f);
        }

        void SetupEnvironment(Camera cam, Light sun)
        {
            // Souls-like dusk: dimmer, colder light and closer fog.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.40f, 0.34f, 0.33f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 130f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.27f, 0.25f, 0.28f);

            sun.transform.rotation = Quaternion.Euler(26f, -38f, 0f);
            sun.color = new Color(0.95f, 0.78f, 0.62f);
            sun.intensity = 0.85f;
            sun.shadows = LightShadows.Soft;

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.52f, 0.46f, 0.50f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.30f, 0.26f, 0.20f));
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.4f);
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 0.95f);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.045f);
                RenderSettings.skybox = sky;
                RenderSettings.sun = sun;
                cam.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.70f, 0.55f, 0.40f);
            }
        }

        void BuildGround()
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
            g.name = "Ground";
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0f, 0f, 28f);
            g.transform.localScale = new Vector3(40f, 1f, 40f);
            g.GetComponent<MeshRenderer>().sharedMaterial = _ground;
        }

        // ---- House -----------------------------------------------------------

        void BuildHouse()
        {
            var root = new GameObject("House").transform;
            root.SetParent(transform, false);
            root.localPosition = HouseCenter;

            PAArt.Box(root, new Vector3(0, 0.05f, 0), new Vector3(8.4f, 0.1f, 8.4f), _floor);

            // walls (thickness 0.25, height 3); door gap in +Z wall
            PAArt.Box(root, new Vector3(0, 1.5f, -4f), new Vector3(8.4f, 3f, 0.25f), _wall);   // south
            PAArt.Box(root, new Vector3(-4f, 1.5f, 0), new Vector3(0.25f, 3f, 8.4f), _wall);   // west
            PAArt.Box(root, new Vector3(4f, 1.5f, 0), new Vector3(0.25f, 3f, 8.4f), _wall);    // east
            PAArt.Box(root, new Vector3(-2.5f, 1.5f, 4f), new Vector3(3.4f, 3f, 0.25f), _wall);// north-left
            PAArt.Box(root, new Vector3(2.5f, 1.5f, 4f), new Vector3(3.4f, 3f, 0.25f), _wall); // north-right
            PAArt.Box(root, new Vector3(0, 2.6f, 4f), new Vector3(1.8f, 0.8f, 0.25f), _wood);  // lintel

            // proper gabled roof. Ridge runs east-west at z=0; slopes fall toward z = +/-.
            // (+X rotation pitches an edge DOWN in Unity, so the ridge-side edge needs the
            //  opposite sign on each slope to meet at the peak.)
            PAArt.Box(root, new Vector3(0f, 4.2f, -2.2f), new Vector3(9.0f, 0.2f, 5.6f), _roof, new Vector3(-26f, 0, 0)); // south slope (full)
            PAArt.Box(root, new Vector3(2.3f, 4.2f, 2.2f), new Vector3(4.4f, 0.2f, 5.6f), _roof, new Vector3(26f, 0, 0));  // north slope (west half collapsed -> ruined + daylight)
            // ridge beam + an exposed rafter over the collapsed gap
            var ridge = PAArt.Box(root, new Vector3(0f, 5.25f, 0f), new Vector3(9.0f, 0.18f, 0.18f), _wood);
            var rafter = PAArt.Box(root, new Vector3(-2.2f, 4.1f, 2.1f), new Vector3(0.18f, 0.18f, 4.8f), _wood, new Vector3(26f, 0, 0));
            PAArt.StripColliders(ridge); PAArt.StripColliders(rafter);
            // a couple of fallen beams on the floor
            var b1 = PAArt.Box(root, new Vector3(1.8f, 0.25f, 1.2f), new Vector3(0.25f, 0.25f, 5f), _wood, new Vector3(0, 24f, 0));
            var b2 = PAArt.Box(root, new Vector3(2.6f, 0.2f, -1.5f), new Vector3(0.22f, 0.22f, 4f), _wood, new Vector3(0, -40f, 0));
            PAArt.StripColliders(b1); PAArt.StripColliders(b2);

            // bed in the SW corner (head toward the south wall)
            PAArt.Box(root, new Vector3(-2.7f, 0.35f, -2.7f), new Vector3(1.5f, 0.5f, 2.5f), _wood);
            PAArt.Box(root, new Vector3(-2.7f, 0.64f, -2.6f), new Vector3(1.35f, 0.18f, 2.2f), _sheet);
            var pillow = PAArt.Box(root, new Vector3(-2.7f, 0.74f, -3.5f), new Vector3(1.1f, 0.16f, 0.55f), _pillow);
            PAArt.StripColliders(pillow);

            // small table + lantern (with a warm point light)
            PAArt.Box(root, new Vector3(2.3f, 0.8f, -2.6f), new Vector3(1.4f, 0.1f, 0.9f), _wood);
            foreach (var lx in new[] { -0.6f, 0.6f })
                foreach (var lz in new[] { -0.35f, 0.35f })
                {
                    var leg = PAArt.Box(root, new Vector3(2.3f + lx, 0.4f, -2.6f + lz), new Vector3(0.1f, 0.8f, 0.1f), _wood);
                    PAArt.StripColliders(leg);
                }
            var lantern = PAArt.Sph(root, new Vector3(2.3f, 1.0f, -2.6f), new Vector3(0.25f, 0.25f, 0.25f), _fire);
            PAArt.StripColliders(lantern);

            var lampGo = new GameObject("Lantern Light");
            lampGo.transform.SetParent(root, false);
            lampGo.transform.localPosition = new Vector3(2.3f, 1.4f, -2.6f);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.7f, 0.4f);
            lamp.range = 9f;
            lamp.intensity = 2.2f;
            lampGo.AddComponent<FlickerLight>().baseIntensity = 2.2f;
        }

        // ---- Path ------------------------------------------------------------

        void BuildPath()
        {
            var root = new GameObject("Path").transform;
            root.SetParent(transform, false);
            Random.InitState(99);
            for (float z = 6f; z < 58f; z += 3.6f)
            {
                float jitter = Mathf.Sin(z * 0.4f) * 0.8f;
                var tile = PAArt.Box(root, new Vector3(jitter, 0.02f, z), new Vector3(2.4f, 0.05f, 3.4f), _path);
                PAArt.StripColliders(tile);
            }
        }

        // ---- Village ---------------------------------------------------------

        void BuildVillage()
        {
            var root = new GameObject("Village").transform;
            root.SetParent(transform, false);

            // Use the RPG poly-pack buildings if present, else a single village model, else huts.
            if (PAModels.Has("Village/rpgpp_lt_building_01"))
            {
                BuildRpgVillage(root);
            }
            else if (PAModels.Has("Village"))
            {
                var v = PAModels.Spawn("Village", root, VillageCenter, PAModels.VillageTuning);
                if (v != null) PAModels.AddMeshColliders(v);
            }
            else
            {
                Vector3[] hutOffsets =
                {
                    new Vector3(-11f, 0f, 4f), new Vector3(11f, 0f, 5f),
                    new Vector3(-9f, 0f, 17f), new Vector3(9f, 0f, 16f),
                    new Vector3(0f, 0f, 21f),
                };
                foreach (var off in hutOffsets) BuildHut(root, VillageCenter + off);

                // perimeter fence posts (decorative)
                Random.InitState(7);
                for (int i = 0; i < 26; i++)
                {
                    float a = i / 26f * Mathf.PI * 2f;
                    Vector3 p = VillageCenter + new Vector3(Mathf.Cos(a) * 16f, 0f, Mathf.Sin(a) * 16f + 6f);
                    if (p.z < VillageCenter.z - 8f) continue; // leave the south entrance open
                    var post = PAArt.Box(root, p + Vector3.up * 0.6f, new Vector3(0.18f, 1.2f, 0.18f), _wood);
                    PAArt.StripColliders(post);
                }
            }

            BuildCampfire(root, VillageCenter + new Vector3(0f, 0f, 6f));

            // drifting embers over the square (Souls atmosphere)
            var embers = new GameObject("Embers");
            embers.transform.SetParent(root, false);
            embers.transform.localPosition = VillageCenter + new Vector3(0f, 0.5f, 4f);
            embers.AddComponent<EmberField>();

            // the quest NPC at the village entrance, facing the path
            var npcRoot = BuildHumanoid(root, VillageCenter + new Vector3(0f, 0f, -9f), 180f, "Villager", _coat, _skin, _hat);
            npcRoot.name = "Villager (NPC)";
            var marker = PAArt.Sph(npcRoot.transform, new Vector3(0f, 2.15f, 0f), new Vector3(0.28f, 0.28f, 0.28f), _marker);
            PAArt.StripColliders(marker);
            Villager = npcRoot.AddComponent<NPC>();
            Villager.Configure(marker.transform);
            SocketWeapon(npcRoot, 0); // the quest-giver carries a pitchfork

            // a few idle villagers for life (with subtle idle motion)
            foreach (var v in new[]
            {
                BuildHumanoid(root, VillageCenter + new Vector3(-6f, 0f, 3f), 90f, "Villager", _coat, _skin, _hat),
                BuildHumanoid(root, VillageCenter + new Vector3(6f, 0f, 4f), -100f, "Villager", _coat, _skin, _hat),
                BuildHumanoid(root, VillageCenter + new Vector3(2f, 0f, 9f), 200f, "Villager", _coat, _skin, _hat),
            })
            {
                var cb = v.AddComponent<Combatant>();
                cb.faction = Faction.Friendly; cb.maxHealth = 100f; cb.health = 100f; cb.regenPerSec = 2f;
                v.AddComponent<AllyAI>();
                AddBodyCollider(v, 1.75f);
                SocketWeapon(v, Random.Range(0, 2));
            }
        }

        // Called when the wave starts: the quest NPC drops its dialogue role and joins the fight.
        public void ActivateVillagerCombat()
        {
            if (Villager == null) return;
            var go = Villager.gameObject;
            Villager.enabled = false;
            if (go.GetComponent<Combatant>() == null)
            {
                var cb = go.AddComponent<Combatant>();
                cb.faction = Faction.Friendly; cb.maxHealth = 100f; cb.health = 100f; cb.regenPerSec = 2f;
            }
            if (go.GetComponent<AllyAI>() == null) go.AddComponent<AllyAI>();
            if (go.GetComponent<CapsuleCollider>() == null) AddBodyCollider(go, 1.75f);
        }

        // Kinematic capsule so the player can't walk through a humanoid.
        static void AddBodyCollider(GameObject go, float height)
        {
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, height * 0.5f, 0f);
            col.height = height;
            col.radius = height * 0.2f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
        }

        // Put a weapon in a villager's hand (kind 0 = pitchfork, 1 = axe).
        void GiveWeapon(Transform holder, int kind)
        {
            Vector3 off = new Vector3(0.28f, 1.0f, 0.2f);
            Vector3 euler = new Vector3(12f, 0f, -18f);
            if (kind == 0) PAArt.BuildPitchfork(holder, off, euler);
            else PAArt.BuildAxe(holder, off, euler);
        }

        // Socket a weapon into an animated humanoid's right hand (falls back to a fixed offset).
        void SocketWeapon(GameObject holder, int kind)
        {
            var anim = holder.GetComponentInChildren<Animator>();
            Transform hand = null, lower = null;
            if (anim != null && anim.isHuman)
            {
                hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                lower = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            }
            if (hand == null) { GiveWeapon(holder.transform, kind); return; }

            var w = kind == 0 ? PAArt.BuildPitchfork(hand, Vector3.zero, Vector3.zero)
                              : PAArt.BuildAxe(hand, Vector3.zero, Vector3.zero);
            float ls = hand.lossyScale.x;
            w.transform.localScale = Vector3.one * (ls > 0.0001f ? 0.8f / ls : 0.8f);
            if (lower != null)
            {
                Vector3 g = (hand.position - lower.position).normalized;
                if (g.sqrMagnitude > 0.0001f)
                    w.transform.rotation = Quaternion.FromToRotation(w.transform.up, g) * w.transform.rotation;
            }
        }

        // The "take this!" moment — sockets an axe into the player's animated hand.
        public void GivePlayerWeapon(PlayerController player)
        {
            if (player != null) player.EquipAxe();
        }

        // Compose a village from the RPG Poly Pack buildings/props (Resources/Models/Village/*).
        void BuildRpgVillage(Transform root)
        {
            void Place(string name, Vector3 worldPos, float yaw, bool collide)
            {
                var go = PAModels.Spawn("Village/" + name, root, worldPos,
                    new PAModels.Tuning(1f, new Vector3(0f, yaw, 0f), Vector3.zero));
                if (go != null && collide) PAModels.AddMeshColliders(go);
            }
            float Face(Vector3 off)
            {
                Vector3 dir = -off; dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) return PAModels.VillageBuildingYaw;
                return Quaternion.LookRotation(dir).eulerAngles.y + PAModels.VillageBuildingYaw;
            }

            Vector3 C = VillageCenter;

            // buildings ringed around the square, doors toward the centre
            Place("rpgpp_lt_building_01", C + new Vector3(-13f, 0f, 4f), Face(new Vector3(-13f, 0, 4f)), true);
            Place("rpgpp_lt_building_02", C + new Vector3(13f, 0f, 5f), Face(new Vector3(13f, 0, 5f)), true);
            Place("rpgpp_lt_building_03", C + new Vector3(-11f, 0f, 18f), Face(new Vector3(-11f, 0, 18f)), true);
            Place("rpgpp_lt_building_04", C + new Vector3(11f, 0f, 18f), Face(new Vector3(11f, 0, 18f)), true);
            Place("rpgpp_lt_building_05", C + new Vector3(0f, 0f, 24f), Face(new Vector3(0f, 0, 24f)), true);

            // well, wagon and market props around the square
            Place("rpgpp_lt_well_01", C + new Vector3(-5f, 0f, 11f), 0f, true);
            Place("rpgpp_lt_wagon_01", C + new Vector3(7f, 0f, 2f), 40f, true);
            Place("rpgpp_lt_table_01", C + new Vector3(-3f, 0f, 1f), 0f, true);
            Place("rpgpp_lt_bench_wood_01", C + new Vector3(-3f, 0f, 3.2f), 180f, true);
            Place("rpgpp_lt_barrel_01", C + new Vector3(4f, 0f, 10f), 0f, true);
            Place("rpgpp_lt_barrel_02", C + new Vector3(4.9f, 0f, 10.8f), 0f, true);
            Place("rpgpp_lt_crate_01", C + new Vector3(-8f, 0f, 2f), 20f, true);
            Place("rpgpp_lt_crate_02", C + new Vector3(-8.7f, 0f, 2.6f), -15f, true);
            Place("rpgpp_lt_trough_01", C + new Vector3(9f, 0f, 7f), 90f, true);
            Place("rpgpp_lt_sack_02_set", C + new Vector3(3f, 0f, 4f), 0f, false);
            Place("rpgpp_lt_banner_01a", C + new Vector3(-12f, 0f, -1f), 0f, false);
            Place("rpgpp_lt_banner_01a", C + new Vector3(12f, 0f, -1f), 0f, false);

            // greenery + fences flanking the entrance
            Place("rpgpp_lt_tree_01", C + new Vector3(-17f, 0f, 14f), 0f, true);
            Place("rpgpp_lt_tree_02", C + new Vector3(17f, 0f, 12f), 30f, true);
            Place("rpgpp_lt_bush_01", C + new Vector3(-9f, 0f, -2f), 0f, false);
            Place("rpgpp_lt_bush_02", C + new Vector3(9f, 0f, -2f), 0f, false);
            Place("rpgpp_lt_fence_wood_01a", C + new Vector3(-7f, 0f, -6f), 90f, true);
            Place("rpgpp_lt_fence_wood_01a", C + new Vector3(7f, 0f, -6f), 90f, true);
        }

        void BuildHut(Transform parent, Vector3 center)
        {
            var hut = new GameObject("Hut").transform;
            hut.SetParent(parent, false);
            hut.localPosition = center;

            PAArt.Box(hut, new Vector3(0, 1.3f, 0), new Vector3(4f, 2.6f, 4f), _hutWall);
            PAArt.Box(hut, new Vector3(0, 3.0f, -1.1f), new Vector3(4.6f, 0.2f, 2.7f), _hutRoof, new Vector3(-32f, 0, 0));
            PAArt.Box(hut, new Vector3(0, 3.0f, 1.1f), new Vector3(4.6f, 0.2f, 2.7f), _hutRoof, new Vector3(32f, 0, 0));

            // dark doorway facing the village centre
            Vector3 dir = VillageCenter - center; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.back;
            dir.Normalize();
            var door = PAArt.Box(hut, dir * 2.02f + Vector3.up * 0.9f,
                new Vector3(1.2f, 1.8f, 0.1f), _wood, Quaternion.LookRotation(dir).eulerAngles);
            PAArt.StripColliders(door);
        }

        void BuildCampfire(Transform parent, Vector3 center)
        {
            var fire = new GameObject("Campfire").transform;
            fire.SetParent(parent, false);
            fire.localPosition = center;

            // Use the imported campfire prefab (logs + fire FX + light) if present.
            if (PAModels.Has("Campfire"))
            {
                PAModels.Spawn("Campfire", fire, Vector3.zero, new PAModels.Tuning(1f, Vector3.zero, Vector3.zero));
                return;
            }

            // stone ring
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                var rock = PAArt.Sph(fire, new Vector3(Mathf.Cos(a) * 0.85f, 0.12f, Mathf.Sin(a) * 0.85f),
                    new Vector3(0.45f, 0.3f, 0.45f), _rock);
                PAArt.StripColliders(rock);
            }
            // charred logs
            var l1 = PAArt.Box(fire, new Vector3(0, 0.18f, 0), new Vector3(0.18f, 0.18f, 1.4f), _wood, new Vector3(0, 25f, 8f));
            var l2 = PAArt.Box(fire, new Vector3(0, 0.18f, 0), new Vector3(0.18f, 0.18f, 1.4f), _wood, new Vector3(0, -40f, -8f));
            var l3 = PAArt.Box(fire, new Vector3(0, 0.30f, 0), new Vector3(0.16f, 0.16f, 1.2f), _wood, new Vector3(0, 80f, 10f));
            PAArt.StripColliders(l1); PAArt.StripColliders(l2); PAArt.StripColliders(l3);

            // reliable, always-upright animated flame (procedural fallback)
            fire.gameObject.AddComponent<FlameFX>();
        }

        // ---- Decorations -----------------------------------------------------

        void BuildDecorations()
        {
            var root = new GameObject("Decorations").transform;
            root.SetParent(transform, false);
            Random.InitState(2024);

            string[] props = { "SM_Rock_1", "SM_Rock_2", "SM_Rock_3", "SM_Rock_4", "SM_Rock_5", "SM_cactus", "SM_Cactus_2" };
            bool haveProps = PAModels.Has("Props/SM_Rock_1");

            int count = haveProps ? 60 : 40;
            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-48f, 48f);
                float z = Random.Range(-22f, 82f);
                // keep the path corridor and the buildings clear
                if (Mathf.Abs(x) < 4f && z > -6f && z < 58f) continue;
                if (Mathf.Abs(x) < 7f && Mathf.Abs(z - HouseCenter.z) < 7f) continue;
                if ((new Vector3(x, 0, z) - VillageCenter).sqrMagnitude < 18f * 18f) continue;

                if (haveProps)
                {
                    string nm = props[Random.Range(0, props.Length)];
                    var t = new PAModels.Tuning(Random.Range(0.7f, 1.8f),
                        new Vector3(0f, Random.Range(0f, 360f), 0f), Vector3.zero);
                    var go = PAModels.Spawn("Props/" + nm, root, new Vector3(x, 0f, z), t);
                    if (go != null && nm.Contains("Rock")) PAModels.AddMeshColliders(go);
                    continue;
                }

                int kind = Random.Range(0, 3);
                if (kind == 0) DeadTree(root, new Vector3(x, 0, z));
                else if (kind == 1)
                {
                    var rock = PAArt.Sph(root, new Vector3(x, Random.Range(0.1f, 0.4f), z),
                        new Vector3(Random.Range(0.6f, 1.8f), Random.Range(0.5f, 1.1f), Random.Range(0.6f, 1.8f)), _rock);
                    PAArt.StripColliders(rock);
                }
                else
                {
                    int n = Random.Range(2, 5);
                    for (int t2 = 0; t2 < n; t2++)
                    {
                        var tuft = PAArt.Box(root,
                            new Vector3(x + Random.Range(-0.6f, 0.6f), 0.25f, z + Random.Range(-0.6f, 0.6f)),
                            new Vector3(0.08f, Random.Range(0.4f, 0.8f), 0.08f),
                            PAArt.Mat(new Color(0.36f, 0.33f, 0.18f)),
                            new Vector3(Random.Range(-12f, 12f), 0, Random.Range(-12f, 12f)));
                        PAArt.StripColliders(tuft);
                    }
                }
            }
        }

        void DeadTree(Transform parent, Vector3 pos)
        {
            var tree = new GameObject("DeadTree").transform;
            tree.SetParent(parent, false);
            tree.localPosition = pos;
            float h = Random.Range(2.5f, 4.5f);
            var trunk = PAArt.Cyl(tree, new Vector3(0, h * 0.5f, 0), new Vector3(0.3f, h * 0.5f, 0.3f), _trunk);
            PAArt.StripColliders(trunk);
            int branches = Random.Range(2, 4);
            for (int i = 0; i < branches; i++)
            {
                float a = Random.Range(0f, 360f);
                var br = PAArt.Cyl(tree, new Vector3(0, h * Random.Range(0.55f, 0.9f), 0),
                    new Vector3(0.12f, Random.Range(0.5f, 1.1f), 0.12f), _trunk,
                    new Vector3(Random.Range(35f, 70f), a, 0));
                PAArt.StripColliders(br);
            }
        }

        // ---- Humanoids -------------------------------------------------------

        GameObject BuildHumanoid(Transform parent, Vector3 pos, float yaw, string modelName,
                                 Material coat, Material skin, Material hat)
        {
            var root = new GameObject("Villager");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localEulerAngles = new Vector3(0, yaw, 0);

            if (modelName != null && PAModels.Has(modelName))
            {
                var m = PAModels.SpawnSized(modelName, root.transform, Vector3.zero,
                    PAModels.VillagerHeight, PAModels.VillagerEuler);
                // Rio is Humanoid -> drive it with the shared humanoid controller (idle/walk/attack/die).
                var anim = m != null ? m.GetComponentInChildren<Animator>() : null;
                if (anim != null)
                {
                    if (anim.GetComponent<PAAnimEvents>() == null) anim.gameObject.AddComponent<PAAnimEvents>();
                    var ctrl = Resources.Load<RuntimeAnimatorController>("Models/PlayerAnim");
                    if (ctrl != null) anim.runtimeAnimatorController = ctrl;
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
            }
            else
            {
                PAArt.Capsule(root.transform, new Vector3(0, 0.9f, 0), new Vector3(0.55f, 0.55f, 0.55f), coat);
                PAArt.Sph(root.transform, new Vector3(0, 1.55f, 0), new Vector3(0.42f, 0.42f, 0.42f), skin);
                PAArt.Box(root.transform, new Vector3(0, 1.74f, 0), new Vector3(0.5f, 0.16f, 0.5f), hat);
                foreach (var lx in new[] { -0.16f, 0.16f })
                    PAArt.Box(root.transform, new Vector3(lx, 0.35f, 0), new Vector3(0.16f, 0.7f, 0.16f), _wood);
            }

            PAArt.StripColliders(root);
            return root;
        }

        // ---- Player & enemies ------------------------------------------------

        public PlayerController SpawnPlayer(Camera cam)
        {
            var p = new GameObject("Player");
            p.transform.position = PlayerSpawn + new Vector3(0, 1.0f, 0);
            p.transform.rotation = Quaternion.Euler(0, PlayerStartYaw, 0);

            var cc = p.AddComponent<CharacterController>();
            cc.height = 1.7f;
            cc.radius = 0.32f;
            cc.center = Vector3.zero;
            cc.slopeLimit = 55f;
            cc.stepOffset = 0.4f;

            // Use the imported character model if present, otherwise a primitive capsule.
            // Shown fully in third person; rendered shadow-only in first person.
            GameObject visualRoot;
            var playerModel = PAModels.SpawnSized("Player", p.transform, new Vector3(0f, -0.85f, 0f),
                PAModels.PlayerHeight, PAModels.PlayerEuler);
            if (playerModel != null)
            {
                PAArt.StripColliders(playerModel);
                visualRoot = playerModel;
            }
            else
            {
                visualRoot = PAArt.Capsule(p.transform, Vector3.zero, new Vector3(0.64f, 0.85f, 0.64f),
                    PAArt.Mat(new Color(0.4f, 0.42f, 0.45f)));
                PAArt.StripColliders(visualRoot);
            }

            var pc = p.AddComponent<PlayerController>();
            pc.Setup(cam, PlayerStartYaw);
            pc.SetVisual(visualRoot);

            var cb = p.AddComponent<Combatant>();
            cb.faction = Faction.Friendly; cb.maxHealth = 100f; cb.health = 100f;
            cb.regenPerSec = 0f; cb.destroyOnDeath = false;   // no HP regen — heal only by respawn
            pc.Combat = cb;

            pc.controlEnabled = false;
            return pc;
        }

        public void SpawnGoblinWave(int count)
        {
            var root = new GameObject("Goblins").transform;
            root.SetParent(transform, false);
            Random.InitState(555);
            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0.5f : i / (count - 1f);
                float x = Mathf.Lerp(-11f, 11f, t) + Random.Range(-1.2f, 1.2f);
                float z = 12f + Random.Range(-2f, 5f);   // north of the square -> they march in
                BuildGoblin(root, VillageCenter + new Vector3(x, 0f, z));
            }
        }

        void BuildGoblin(Transform parent, Vector3 pos)
        {
            var root = new GameObject("Goblin");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;

            var body = new GameObject("Body").transform;   // child 0 -> used for bobbing
            body.SetParent(root.transform, false);

            // Use the skeleton model (auto-sized to EnemyHeight), else a primitive enemy.
            var model = PAModels.SpawnSized("Skeleton", body, Vector3.zero, PAModels.EnemyHeight, PAModels.EnemyEuler);
            float h = PAModels.EnemyHeight;
            if (model == null)
            {
                PAArt.Capsule(body, new Vector3(0, 0.8f, 0), new Vector3(0.5f, 0.5f, 0.5f), _goblin);
                PAArt.Sph(body, new Vector3(0, 1.35f, 0), new Vector3(0.4f, 0.4f, 0.4f), _goblinDark);
                PAArt.Sph(body, new Vector3(-0.12f, 1.4f, 0.32f), new Vector3(0.08f, 0.08f, 0.08f), _eye);
                PAArt.Sph(body, new Vector3(0.12f, 1.4f, 0.32f), new Vector3(0.08f, 0.08f, 0.08f), _eye);
                PAArt.Box(body, new Vector3(0.35f, 0.7f, 0.1f), new Vector3(0.12f, 0.12f, 1.0f), _wood,
                    new Vector3(70f, 0, 0)); // crude club
                h = 1.7f;
            }
            else
            {
                var sa = model.GetComponentInChildren<Animator>();
                if (sa != null)
                {
                    var ctrl = Resources.Load<RuntimeAnimatorController>("Models/SkeletonAnim");
                    if (ctrl != null) sa.runtimeAnimatorController = ctrl;
                }
            }

            PAArt.StripColliders(root);

            // collision so the player can't walk through them (kinematic = moved by script)
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, h * 0.5f, 0f);
            col.height = h;
            col.radius = h * 0.22f;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var cbt = root.AddComponent<Combatant>();
            cbt.faction = Faction.Enemy; cbt.maxHealth = 100f; cbt.health = 100f;

            var g = root.AddComponent<Goblin>();
            g.speed = Random.Range(1.9f, 2.6f);
        }
    }

    // Tiny glowing motes drifting upward over an area (Dark-Souls ember ambience).
    public class EmberField : MonoBehaviour
    {
        const int Count = 26;
        Transform[] _e; float[] _p; Vector3[] _base;

        void Start()
        {
            var mat = PAArt.MatEmissive(new Color(1f, 0.45f, 0.15f), 2.2f);
            _e = new Transform[Count]; _p = new float[Count]; _base = new Vector3[Count];
            for (int i = 0; i < Count; i++)
            {
                var s = PAArt.Sph(transform, Vector3.zero, Vector3.one * 0.05f, mat);
                PAArt.StripColliders(s);
                _e[i] = s.transform;
                _p[i] = Random.value;
                _base[i] = new Vector3(Random.Range(-13f, 13f), 0f, Random.Range(-10f, 13f));
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < Count; i++)
            {
                _p[i] += dt * 0.10f;
                if (_p[i] > 1f) { _p[i] -= 1f; _base[i] = new Vector3(Random.Range(-13f, 13f), 0f, Random.Range(-10f, 13f)); }
                float k = _p[i];
                float sway = Mathf.Sin((k * 7f + i) * 2f) * 0.6f;
                _e[i].localPosition = _base[i] + new Vector3(sway, k * 6f, Mathf.Cos((k * 5f + i)) * 0.4f);
                _e[i].localScale = Vector3.one * 0.05f * (1f - k * 0.8f);
            }
        }
    }

    // Small warm flicker for fire/lantern lights.
    public class FlickerLight : MonoBehaviour
    {
        public float baseIntensity = 2f;
        Light _l;
        float _seed;
        void Awake() { _l = GetComponent<Light>(); _seed = Random.value * 10f; }
        void Update()
        {
            if (_l == null) return;
            float n = Mathf.PerlinNoise(_seed, Time.time * 6f);
            _l.intensity = baseIntensity * (0.78f + n * 0.35f);
        }
    }
}
