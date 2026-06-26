using UnityEngine;

namespace PostApoc
{
    // Loads external 3D models if present, otherwise the game uses primitive placeholders.
    // Put imported models in Assets/Resources/Models/ named exactly Village / Goblin / Player.
    //
    // Player and goblins are AUTO-SIZED to a target height at runtime (measured from their
    // actual bounds), so it doesn't matter what scale they were imported at.
    public static class PAModels
    {
        public struct Tuning
        {
            public float scale; public Vector3 euler; public Vector3 offset;
            public Tuning(float s, Vector3 e, Vector3 o) { scale = s; euler = e; offset = o; }
        }

        // ---- TUNE HERE ------------------------------------------------------
        public static Tuning VillageTuning = new Tuning(1f, Vector3.zero, Vector3.zero);

        public static float PlayerHeight = 1.8f;   // metres
        public static float EnemyHeight = 1.7f;    // skeleton
        public static float VillagerHeight = 1.75f;
        public static Vector3 PlayerEuler = Vector3.zero;  // set Y=180 if it faces backward
        public static Vector3 EnemyEuler = Vector3.zero;
        public static Vector3 VillagerEuler = Vector3.zero; // set Y=180 / X=-90 if villagers face wrong
        public static float VillageBuildingYaw = 0f;        // add 180 if building doors face outward
        // ---------------------------------------------------------------------

        public static GameObject Load(string name) => Resources.Load<GameObject>("Models/" + name);
        public static bool Has(string name) => Load(name) != null;

        // Manual placement (used for the village model).
        public static GameObject Spawn(string name, Transform parent, Vector3 pos, Tuning t)
        {
            var prefab = Load(name);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent);
            go.name = name;
            go.transform.localPosition = pos + t.offset;
            go.transform.localRotation = Quaternion.Euler(t.euler);
            go.transform.localScale = Vector3.one * t.scale;
            return go;
        }

        // Instantiate, then rescale to an exact height and drop the feet to 'footPos'
        // (in parent space). Import scale is irrelevant.
        public static GameObject SpawnSized(string name, Transform parent, Vector3 footPos, float targetHeight, Vector3 euler)
        {
            var prefab = Load(name);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent);
            go.name = name;
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(euler);

            Bounds b = ComputeBounds(go);
            if (b.size.y > 1e-4f)
                go.transform.localScale = Vector3.one * (targetHeight / b.size.y);

            b = ComputeBounds(go);
            Vector3 desiredFeet = parent != null ? parent.TransformPoint(footPos) : footPos;
            Vector3 currentFeet = new Vector3(b.center.x, b.min.y, b.center.z);
            go.transform.position += desiredFeet - currentFeet;
            return go;
        }

        public static void AddMeshColliders(GameObject root)
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var mc = mf.GetComponent<MeshCollider>();
                if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        // World-space AABB built from mesh bounds (not cached Renderer.bounds, so it's
        // correct immediately after Instantiate/scale).
        static Bounds ComputeBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            bool has = false; Bounds total = new Bounds();
            foreach (var r in rends)
            {
                Bounds wb;
                if (r is SkinnedMeshRenderer)
                {
                    wb = r.bounds; // world AABB from the skinned mesh's local bounds
                }
                else
                {
                    var mf = r.GetComponent<MeshFilter>();
                    wb = (mf != null && mf.sharedMesh != null)
                        ? TransformBounds(r.transform.localToWorldMatrix, mf.sharedMesh.bounds)
                        : r.bounds;
                }
                if (!has) { total = wb; has = true; } else total.Encapsulate(wb);
            }
            return has ? total : new Bounds(go.transform.position, Vector3.zero);
        }

        static Bounds TransformBounds(Matrix4x4 m, Bounds b)
        {
            Vector3 c = m.MultiplyPoint3x4(b.center);
            Vector3 e = b.extents;
            Vector3 ax = m.MultiplyVector(new Vector3(e.x, 0, 0));
            Vector3 ay = m.MultiplyVector(new Vector3(0, e.y, 0));
            Vector3 az = m.MultiplyVector(new Vector3(0, 0, e.z));
            Vector3 ne = new Vector3(
                Mathf.Abs(ax.x) + Mathf.Abs(ay.x) + Mathf.Abs(az.x),
                Mathf.Abs(ax.y) + Mathf.Abs(ay.y) + Mathf.Abs(az.y),
                Mathf.Abs(ax.z) + Mathf.Abs(ay.z) + Mathf.Abs(az.z));
            return new Bounds(c, ne * 2f);
        }
    }
}
