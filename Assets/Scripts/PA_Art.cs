using System.Collections.Generic;
using UnityEngine;

namespace PostApoc
{
    // Procedural materials, primitive builders and generated textures.
    // No external art assets are needed; everything is made in code so the project
    // runs as-is. Materials target URP/Lit (this is a URP project).
    public static class PAArt
    {
        static Shader _lit;
        public static Shader Lit
        {
            get
            {
                if (_lit == null)
                {
                    _lit = Shader.Find("Universal Render Pipeline/Lit");
                    if (_lit == null) _lit = Shader.Find("Standard");
                    if (_lit == null) _lit = Shader.Find("Universal Render Pipeline/Unlit");
                    if (_lit == null) _lit = Shader.Find("Sprites/Default");
                }
                return _lit;
            }
        }

        static Color C(float r, float g, float b) { return new Color(r, g, b); }

        // ---- Materials -------------------------------------------------------

        public static Material Mat(Color c, float smoothness = 0.05f, float metallic = 0f)
        {
            var m = new Material(Lit);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            return m;
        }

        public static Material MatTex(Texture2D tex, Color tint, Vector2 tiling, float smoothness = 0.05f)
        {
            var m = Mat(tint, smoothness);
            if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tiling); }
            m.mainTexture = tex;
            m.mainTextureScale = tiling;
            return m;
        }

        public static Material MatEmissive(Color c, float intensity = 1.6f)
        {
            var m = Mat(c, 0.1f);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * intensity);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return m;
        }

        // ---- Weapons (procedural, held: grip at local origin, shaft up +Y) ----

        public static GameObject BuildAxe(Transform parent, Vector3 pos, Vector3 euler)
        {
            var wood = Mat(new Color(0.32f, 0.20f, 0.12f));
            var metal = Mat(new Color(0.62f, 0.63f, 0.67f), 0.45f, 0.6f);
            var root = new GameObject("Axe");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localEulerAngles = euler;
            StripColliders(Cyl(root.transform, new Vector3(0, 0.45f, 0), new Vector3(0.035f, 0.5f, 0.035f), wood));
            StripColliders(Box(root.transform, new Vector3(0.02f, 0.86f, 0.07f), new Vector3(0.05f, 0.16f, 0.26f), metal));
            StripColliders(Box(root.transform, new Vector3(0.02f, 0.86f, -0.05f), new Vector3(0.05f, 0.16f, 0.12f), metal));
            return root;
        }

        public static GameObject BuildPitchfork(Transform parent, Vector3 pos, Vector3 euler)
        {
            var wood = Mat(new Color(0.32f, 0.20f, 0.12f));
            var metal = Mat(new Color(0.60f, 0.61f, 0.65f), 0.45f, 0.6f);
            var root = new GameObject("Pitchfork");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localEulerAngles = euler;
            StripColliders(Cyl(root.transform, new Vector3(0, 0.6f, 0), new Vector3(0.035f, 0.7f, 0.035f), wood));
            StripColliders(Box(root.transform, new Vector3(0, 1.28f, 0), new Vector3(0.22f, 0.03f, 0.03f), metal));
            for (int i = -1; i <= 1; i++)
                StripColliders(Box(root.transform, new Vector3(i * 0.09f, 1.45f, 0), new Vector3(0.02f, 0.28f, 0.02f), metal));
            return root;
        }

        // ---- Swing VFX (additive crescent slash) -----------------------------

        static Mesh _slash;
        public static Mesh SlashMesh()
        {
            if (_slash != null) return _slash;
            int seg = 18;
            float a0 = -65f * Mathf.Deg2Rad, a1 = 65f * Mathf.Deg2Rad, ri = 0.5f, ro = 1.5f;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg, a = Mathf.Lerp(a0, a1, t), cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                v.Add(new Vector3(cs * ri, sn * ri, 0)); v.Add(new Vector3(cs * ro, sn * ro, 0));
                uv.Add(new Vector2(t, 0)); uv.Add(new Vector2(t, 1));
            }
            for (int i = 0; i < seg; i++)
            {
                int b = i * 2;
                tr.Add(b); tr.Add(b + 1); tr.Add(b + 2); tr.Add(b + 1); tr.Add(b + 3); tr.Add(b + 2);
                tr.Add(b); tr.Add(b + 2); tr.Add(b + 1); tr.Add(b + 1); tr.Add(b + 2); tr.Add(b + 3); // back faces
            }
            _slash = new Mesh();
            _slash.SetVertices(v); _slash.SetUVs(0, uv); _slash.SetTriangles(tr, 0); _slash.RecalculateBounds();
            return _slash;
        }

        public static Material AdditiveUnlit(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        // ---- Primitive builders ---------------------------------------------

        public static GameObject Box(Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Setup(g, parent, pos, scale, euler, mat);
            return g;
        }

        public static GameObject Cyl(Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Setup(g, parent, pos, scale, euler, mat);
            return g;
        }

        public static GameObject Sph(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Setup(g, parent, pos, scale, Vector3.zero, mat);
            return g;
        }

        public static GameObject Capsule(Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Setup(g, parent, pos, scale, euler, mat);
            return g;
        }

        static void Setup(GameObject g, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material mat)
        {
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.transform.localEulerAngles = euler;
            var r = g.GetComponent<MeshRenderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        public static void StripColliders(GameObject root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(col);
        }

        // ---- Textures --------------------------------------------------------

        public static Texture2D Pixel(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, c);
            t.wrapMode = TextureWrapMode.Repeat;
            t.Apply();
            return t;
        }

        // Deterministic 0..1 hash.
        static float Hash(int a, int b)
        {
            unchecked
            {
                uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
            }
        }

        // Top of the horizon silhouette (0..1) for a given column / variant.
        // Periodic in u so the texture scrolls seamlessly.
        static float SilTop(float u, int type, float hy)
        {
            switch (type)
            {
                case 1: // city buildings
                {
                    int n = 22; int col = Mathf.FloorToInt(u * n);
                    float hgt = 0.05f + Hash(col, 7) * 0.24f;
                    if (Hash(col, 3) > 0.85f) hgt = 0.02f;
                    return hy + hgt;
                }
                case 2: // dead forest
                {
                    int n = 64; int col = Mathf.FloorToInt(u * n);
                    float hgt = 0.03f + Hash(col, 11) * 0.11f;
                    if (Hash(col, 5) > 0.5f) hgt *= 0.5f;
                    return hy + hgt;
                }
                case 3: // jagged mountains
                {
                    float m = 0.11f * Mathf.Abs(Mathf.Sin(u * Mathf.PI * 3f))
                            + 0.06f * Mathf.Abs(Mathf.Sin(u * Mathf.PI * 7f + 1f)) + 0.04f;
                    return hy + m;
                }
                default: // rolling dead hills
                {
                    float m = 0.03f + 0.02f * Mathf.Sin(u * Mathf.PI * 4f) + 0.015f * Mathf.Sin(u * Mathf.PI * 9f);
                    return hy + m;
                }
            }
        }

        // Full post-apocalyptic backdrop. Variants 0..3.
        public static Texture2D Background(int v)
        {
            int w = 384, h = 216;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapModeU = TextureWrapMode.Repeat;
            tex.wrapModeV = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color skyTop, skyHor, gndNear, gndFar, sil, sunCol;
            int silType; float sunU, sunV;

            switch (v)
            {
                case 1: // Ruins of the Old World
                    skyTop = C(0.18f, 0.06f, 0.10f); skyHor = C(0.88f, 0.32f, 0.12f);
                    gndFar = C(0.18f, 0.10f, 0.10f); gndNear = C(0.09f, 0.06f, 0.06f);
                    sil = C(0.05f, 0.035f, 0.05f); silType = 1;
                    sunCol = C(1f, 0.45f, 0.20f); sunU = 0.70f; sunV = 0.55f; break;
                case 2: // Toxic Mire
                    skyTop = C(0.14f, 0.20f, 0.15f); skyHor = C(0.56f, 0.67f, 0.33f);
                    gndFar = C(0.20f, 0.24f, 0.15f); gndNear = C(0.11f, 0.14f, 0.08f);
                    sil = C(0.05f, 0.07f, 0.05f); silType = 2;
                    sunCol = C(0.75f, 0.92f, 0.45f); sunU = 0.72f; sunV = 0.62f; break;
                case 3: // Frozen Silence
                    skyTop = C(0.28f, 0.34f, 0.44f); skyHor = C(0.80f, 0.85f, 0.92f);
                    gndFar = C(0.64f, 0.68f, 0.74f); gndNear = C(0.40f, 0.44f, 0.50f);
                    sil = C(0.22f, 0.27f, 0.34f); silType = 3;
                    sunCol = C(0.92f, 0.96f, 1f); sunU = 0.68f; sunV = 0.66f; break;
                default: // Ashen Plains
                    skyTop = C(0.26f, 0.18f, 0.16f); skyHor = C(0.94f, 0.57f, 0.27f);
                    gndFar = C(0.42f, 0.33f, 0.20f); gndNear = C(0.22f, 0.17f, 0.11f);
                    sil = C(0.14f, 0.10f, 0.07f); silType = 0;
                    sunCol = C(1f, 0.73f, 0.37f); sunU = 0.74f; sunV = 0.60f; break;
            }

            float hy = 0.46f;
            float[] silTop = new float[w];
            for (int x = 0; x < w; x++) silTop[x] = SilTop(x / (float)w, silType, hy);

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)h;
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w;
                    Color c;
                    if (ny >= hy)
                    {
                        float t = (ny - hy) / (1f - hy);
                        c = Color.Lerp(skyHor, skyTop, Mathf.Pow(t, 0.85f));

                        float cloud = Mathf.PerlinNoise(u * 5f + v * 10f, ny * 6f);
                        cloud = Mathf.SmoothStep(0.55f, 0.82f, cloud) * (1f - t) * 0.30f;
                        c = Color.Lerp(c, skyHor * 1.12f, cloud);

                        float du = (u - sunU);
                        float dv = (ny - sunV) * 1.4f;
                        float d = Mathf.Sqrt(du * du + dv * dv);
                        float glow = Mathf.Clamp01(1f - d / 0.5f); glow *= glow;
                        c = Color.Lerp(c, sunCol, glow * 0.8f);
                        float disc = Mathf.Clamp01(1f - d / 0.055f);
                        c = Color.Lerp(c, sunCol * 1.3f, disc);

                        if (ny < silTop[x])
                        {
                            float edge = Mathf.Clamp01((silTop[x] - ny) / 0.02f);
                            c = Color.Lerp(c, sil, edge);
                            if (silType == 1 && Hash(x / 3, Mathf.FloorToInt(ny * 120f)) > 0.93f)
                                c = Color.Lerp(c, C(1f, 0.7f, 0.3f), 0.7f); // lit windows
                        }
                    }
                    else
                    {
                        float t = ny / hy;
                        c = Color.Lerp(gndNear, gndFar, t);
                        float n = Mathf.PerlinNoise(u * 40f, ny * 40f);
                        c = Color.Lerp(c, c * 0.7f, (n - 0.5f) * 0.6f + 0.3f);
                        float streak = Mathf.PerlinNoise(u * 120f + 10f, ny * 8f);
                        if (streak > 0.62f) c = Color.Lerp(c, gndFar * 1.1f, 0.25f);
                    }

                    float grain = (Hash(x * 7 + y * 13, x * 3 - y * 5) - 0.5f) * 0.05f;
                    c.r += grain; c.g += grain; c.b += grain;
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // Windswept grass silhouette strip (transparent above the blades, tiles horizontally).
        // Tinted at draw time per scene.
        public static Texture2D Grass()
        {
            int w = 256, h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapModeU = TextureWrapMode.Repeat;
            tex.wrapModeV = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

            int blades = 90;
            for (int i = 0; i < blades; i++)
            {
                float bu = i / (float)blades;
                int bx = Mathf.RoundToInt(bu * w) % w;
                float bh = (0.40f + Hash(i, 1) * 0.55f) * h;
                int lean = (int)((Hash(i, 2) - 0.5f) * 7f);
                float width = 1.2f + Hash(i, 3) * 1.9f;

                int top = Mathf.Min(h - 1, Mathf.RoundToInt(bh));
                for (int y = 0; y < top; y++)
                {
                    float ty = y / bh;
                    int cx = bx + (int)(lean * ty);
                    int ww = Mathf.Max(1, (int)(width * (1f - ty * 0.7f)));
                    float shade = 0.45f + ty * 0.55f;
                    byte r = (byte)(22 * shade), g = (byte)(30 * shade), b = (byte)(16 * shade);
                    for (int dx = -ww; dx <= ww; dx++)
                    {
                        int xx = ((cx + dx) % w + w) % w;
                        px[y * w + xx] = new Color32(r, g, b, 255);
                    }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        public static Texture2D Vignette()
        {
            int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x / (float)s - 0.5f) * 2f;
                    float dy = (y / (float)s - 0.5f) * 2f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                    float a = Mathf.SmoothStep(0.55f, 1f, d) * 0.78f;
                    px[y * s + x] = new Color32(0, 0, 0, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        public static Texture2D Dirt()
        {
            int s = 128;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.6f
                            + Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.4f;
                    float cr = (Hash(x, y) - 0.5f) * 0.08f;
                    Color baseC = Color.Lerp(C(0.30f, 0.24f, 0.16f), C(0.43f, 0.34f, 0.22f), n)
                                + new Color(cr, cr, cr);
                    float crack = Mathf.PerlinNoise(x * 0.15f + 5f, y * 0.15f + 9f);
                    if (crack > 0.78f) baseC *= 0.6f;
                    baseC.a = 1f;
                    px[y * s + x] = baseC;
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ---- Dark-Souls-style theme textures (deep crimson/purple, cracked ruins) ----

        public static Texture2D ThemePanel()
        {
            int w = 256, h = 256, B = 40;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            Color centerDark = C(0.05f, 0.035f, 0.055f), centerEdge = C(0.09f, 0.05f, 0.08f);
            Color frameA = C(0.16f, 0.05f, 0.09f), frameB = C(0.10f, 0.06f, 0.15f);
            Color rim = C(0.55f, 0.12f, 0.12f), crackDark = C(0.02f, 0.01f, 0.02f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c; float a;
                    if (d >= B) { float ct = Mathf.Clamp01((d - B) / 70f); c = Color.Lerp(centerEdge, centerDark, ct); a = 0.93f; }
                    else
                    {
                        float bt = d / (float)B;
                        c = Color.Lerp(frameA, frameB, (x + y) / (float)(w + h));
                        if (bt < 0.12f) c *= 0.45f;
                        else if (bt > 0.80f && bt < 0.95f) c = Color.Lerp(c, rim, 0.6f);
                        a = 1f;
                    }
                    float n = Mathf.PerlinNoise(x * 0.05f + 3f, y * 0.05f + 7f);
                    float crack = Mathf.Pow(1f - Mathf.Abs(n - 0.5f) * 2f, 10f);
                    float ef = d < B ? 1f : Mathf.Clamp01(1f - (d - B) / 26f);
                    c = Color.Lerp(c, crackDark, crack * ef * 0.85f);
                    float g = (Hash(x * 3 + y, x - y * 5) - 0.5f) * 0.04f; c.r += g; c.g += g; c.b += g;
                    c.a = a; px[y * w + x] = c;
                }
            t.SetPixels32(px); t.Apply(); return t;
        }

        public static Texture2D ThemeButton(bool hover)
        {
            int w = 256, h = 96, B = 26;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            Color body = hover ? C(0.30f, 0.06f, 0.08f) : C(0.11f, 0.04f, 0.07f);
            Color ember = C(0.55f, 0.14f, 0.06f);
            Color frameA = hover ? C(0.6f, 0.16f, 0.12f) : C(0.17f, 0.05f, 0.09f);
            Color frameB = hover ? C(0.35f, 0.10f, 0.20f) : C(0.11f, 0.06f, 0.15f);
            Color rim = hover ? C(0.95f, 0.45f, 0.20f) : C(0.45f, 0.10f, 0.12f);
            Color crackDark = C(0.02f, 0.01f, 0.02f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c; float a;
                    if (d >= B)
                    {
                        float vy = y / (float)h;
                        c = body;
                        if (hover) c = Color.Lerp(body, ember, Mathf.Pow(1f - vy, 2f) * 0.7f);
                        a = 0.96f;
                    }
                    else
                    {
                        float bt = d / (float)B;
                        c = Color.Lerp(frameA, frameB, (x + y) / (float)(w + h));
                        if (bt < 0.16f) c *= 0.5f;
                        else if (bt > 0.7f) c = Color.Lerp(c, rim, 0.6f);
                        a = 1f;
                    }
                    float n = Mathf.PerlinNoise(x * 0.06f + 1f, y * 0.06f + 5f);
                    float crack = Mathf.Pow(1f - Mathf.Abs(n - 0.5f) * 2f, 12f);
                    float ef = d < B ? 1f : 0.4f;
                    c = Color.Lerp(c, crackDark, crack * ef * 0.7f);
                    float g = (Hash(x * 5 - y, x + y * 3) - 0.5f) * 0.04f; c.r += g; c.g += g; c.b += g;
                    c.a = a; px[y * w + x] = c;
                }
            t.SetPixels32(px); t.Apply(); return t;
        }

        public static Texture2D ThemeBarFill()
        {
            int w = 8, h = 16;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float vy = y / (float)(h - 1);
                Color c = Color.Lerp(C(0.40f, 0.04f, 0.05f), C(0.80f, 0.16f, 0.14f), vy);
                if (vy > 0.82f) c = Color.Lerp(c, C(0.95f, 0.35f, 0.25f), 0.5f);
                c.a = 1f;
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            t.SetPixels32(px); t.Apply(); return t;
        }

        public static Texture2D ThemeVignette()
        {
            int s = 256;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            Color edge = C(0.13f, 0.0f, 0.06f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x / (float)s - 0.5f) * 2f, dy = (y / (float)s - 0.5f) * 2f;
                    float dd = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                    Color c = edge; c.a = Mathf.SmoothStep(0.45f, 1f, dd) * 0.85f;
                    px[y * s + x] = c;
                }
            t.SetPixels32(px); t.Apply(); return t;
        }
    }
}
