using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// Bakes the three seamless, tileable map backdrops (M15) — Milky Way / Crimson Nebula /
    /// Supernova. Editor-only content generation; the results are committed PNGs, this just
    /// (re)creates them. Menu: SpaceSurvivors/Build/M15 Backdrop Textures.
    /// </summary>
    internal static class BackdropTextureBaker
    {
        private const string Dir = "Assets/_Project/Art/Sprites/Generated/";
        private const int Size = 512;
        private const float Ppu = 18f;
        // Backdrops are drawn on a near-black sky, so the clouds need some presence — but the
        // map-select screen shows them lit, so keep this modest and dim via the tint alpha.
        private const float Boost = 1.25f;

        [MenuItem("SpaceSurvivors/Build/M15 Backdrop Textures")]
        public static void Bake()
        {
            Directory.CreateDirectory(Dir);
            BakeMilkyWay();
            BakeNebula();
            BakeSupernova();
            AssetDatabase.Refresh();
            Debug.Log("[BackdropTextureBaker] baked 3 backdrops.");
        }

        // ---------------------------------------------------------------- maps

        private static void BakeMilkyWay()
        {
            var rng = new System.Random(9111);
            var px = Fill(new Color(0f, 0f, 0f, 0f));

            // The galactic band = a diagonal stripe of overlapping soft blobs (wrapped), so the
            // sky stays mostly dark and only the "river" glows.
            float angle = 0.5f;
            Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 nrm = new(-dir.y, dir.x);
            var centre = new Vector2(Size * 0.5f, Size * 0.5f);

            for (int i = 0; i < 60; i++)
            {
                float t = (float)rng.NextDouble() - 0.5f;
                float off = (float)(rng.NextDouble() - 0.5) * 150f;
                Vector2 c = centre + dir * (t * Size * 1.2f) + nrm * off;
                float fade = 1f - Mathf.Clamp01(Mathf.Abs(off) / 110f);
                Blob(px, rng, c.x, c.y, Rand(rng, 55, 120),
                    new Color(0.52f, 0.60f, 0.90f), (0.05f + 0.05f * (float)rng.NextDouble()) * fade);
            }
            // warm core highlights + dark dust lanes threading the band
            for (int i = 0; i < 14; i++)
            {
                float t = (float)rng.NextDouble() - 0.5f;
                Vector2 c = centre + dir * (t * Size * 1.1f) + nrm * ((float)(rng.NextDouble() - 0.5) * 60f);
                Blob(px, rng, c.x, c.y, Rand(rng, 25, 60), new Color(0.85f, 0.78f, 0.62f), 0.06f);
            }
            for (int i = 0; i < 12; i++)
            {
                float t = (float)rng.NextDouble() - 0.5f;
                Vector2 c = centre + dir * (t * Size * 1.1f) + nrm * ((float)(rng.NextDouble() - 0.5) * 80f);
                Blob(px, rng, c.x, c.y, Rand(rng, 30, 70), new Color(0.03f, 0.03f, 0.06f), -0.12f);
            }

            Stars(px, rng, 120, 0.35f, 0.9f, 0.04f, 0.12f, new Color(0.8f, 0.85f, 1f));
            Stars(px, rng, 18, 0.9f, 1.6f, 0.22f, 0.45f, new Color(0.95f, 0.97f, 1f));
            Save(px, "Backdrop_MilkyWay.png");
        }

        private static void BakeNebula()
        {
            var rng = new System.Random(4242);
            var px = Fill(new Color(0.03f, 0.005f, 0.02f, 0f));

            Color[] warm =
            {
                new(0.75f, 0.10f, 0.18f), new(0.55f, 0.06f, 0.30f),
                new(0.85f, 0.28f, 0.15f), new(0.35f, 0.04f, 0.22f),
            };
            for (int i = 0; i < 70; i++)
            {
                Color col = warm[rng.Next(warm.Length)];
                Blob(px, rng, (float)rng.NextDouble() * Size, (float)rng.NextDouble() * Size,
                    Rand(rng, 55, 150), col, 0.05f + 0.12f * (float)rng.NextDouble());
            }
            // carve voids
            for (int i = 0; i < 24; i++)
                Blob(px, rng, (float)rng.NextDouble() * Size, (float)rng.NextDouble() * Size,
                    Rand(rng, 40, 110), new Color(0.02f, 0.01f, 0.02f), -0.16f);
            // hot cores
            for (int i = 0; i < 14; i++)
                Blob(px, rng, (float)rng.NextDouble() * Size, (float)rng.NextDouble() * Size,
                    Rand(rng, 18, 45), new Color(1f, 0.72f, 0.55f), 0.16f);

            Stars(px, rng, 120, 0.35f, 0.9f, 0.04f, 0.12f, new Color(1f, 0.85f, 0.8f));
            Stars(px, rng, 16, 0.9f, 1.5f, 0.22f, 0.42f, new Color(1f, 0.95f, 0.9f));
            Save(px, "Backdrop_Nebula.png");
        }

        private static void BakeSupernova()
        {
            var rng = new System.Random(7007);
            var px = Fill(new Color(0.02f, 0.02f, 0.04f, 0f));

            // a diffuse hot haze
            for (int i = 0; i < 40; i++)
                Blob(px, rng, (float)rng.NextDouble() * Size, (float)rng.NextDouble() * Size,
                    Rand(rng, 60, 160), new Color(0.55f, 0.28f, 0.12f), 0.05f + 0.06f * (float)rng.NextDouble());

            // several bright bursts with a faint shock ring
            for (int i = 0; i < 7; i++)
            {
                float cx = (float)rng.NextDouble() * Size, cy = (float)rng.NextDouble() * Size;
                float core = Rand(rng, 16, 34);
                Blob(px, rng, cx, cy, core * 2.6f, new Color(1f, 0.55f, 0.20f), 0.14f);
                Blob(px, rng, cx, cy, core, new Color(1f, 0.95f, 0.85f), 0.5f);
                Ring(px, cx, cy, core * 3.4f, core * 0.5f, new Color(1f, 0.72f, 0.4f), 0.12f);
                Ring(px, cx, cy, core * 5.2f, core * 0.7f, new Color(0.9f, 0.4f, 0.5f), 0.06f);
            }

            Stars(px, rng, 110, 0.35f, 0.9f, 0.04f, 0.12f, new Color(1f, 0.9f, 0.85f));
            Stars(px, rng, 14, 0.9f, 1.5f, 0.22f, 0.42f, new Color(1f, 0.97f, 0.92f));
            Save(px, "Backdrop_Supernova.png");
        }

        // ---------------------------------------------------------------- primitives

        private static Color[] Fill(Color c)
        {
            var px = new Color[Size * Size];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            return px;
        }

        private static int Wrap(int v) => ((v % Size) + Size) % Size;

        private static void Add(Color[] px, int x, int y, Color c, float a)
        {
            int idx = Wrap(y) * Size + Wrap(x);
            var p = px[idx];
            px[idx] = new Color(
                p.r + c.r * a, p.g + c.g * a, p.b + c.b * a,
                Mathf.Clamp01(p.a + Mathf.Abs(a)));
        }

        private static void Blob(Color[] px, System.Random rng, float cx, float cy, float r, Color c, float strength)
        {
            strength *= strength > 0f ? Boost : 1f;
            int ir = Mathf.CeilToInt(r);
            for (int dy = -ir; dy <= ir; dy++)
            for (int dx = -ir; dx <= ir; dx++)
            {
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > r) continue;
                float f = 1f - dist / r;
                f = f * f * (3f - 2f * f); // smoothstep
                Add(px, Mathf.RoundToInt(cx + dx), Mathf.RoundToInt(cy + dy), c, strength * f);
            }
        }

        private static void Ring(Color[] px, float cx, float cy, float radius, float thickness, Color c, float strength)
        {
            strength *= Boost;
            int ir = Mathf.CeilToInt(radius + thickness);
            for (int dy = -ir; dy <= ir; dy++)
            for (int dx = -ir; dx <= ir; dx++)
            {
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float f = 1f - Mathf.Abs(dist - radius) / thickness;
                if (f <= 0f) continue;
                f = f * f * (3f - 2f * f);
                Add(px, Mathf.RoundToInt(cx + dx), Mathf.RoundToInt(cy + dy), c, strength * f);
            }
        }

        private static void Stars(Color[] px, System.Random rng, int count, float minR, float maxR,
            float minA, float maxA, Color tint)
        {
            for (int i = 0; i < count; i++)
            {
                float x = (float)rng.NextDouble() * Size;
                float y = (float)rng.NextDouble() * Size;
                float r = Mathf.Lerp(minR, maxR, (float)rng.NextDouble());
                float a = Mathf.Lerp(minA, maxA, (float)rng.NextDouble());
                Blob(px, rng, x, y, Mathf.Max(1f, r), tint, a);
            }
        }

        private static float Rand(System.Random rng, float lo, float hi)
            => Mathf.Lerp(lo, hi, (float)rng.NextDouble());

        private static void Save(Color[] px, string file)
        {
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                px[i] = new Color(Mathf.Clamp01(p.r), Mathf.Clamp01(p.g), Mathf.Clamp01(p.b), Mathf.Clamp01(p.a));
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();

            string path = Dir + file;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = Ppu;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = true;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
    }
}
