using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// Bakes a seamless, tileable star texture for <see cref="SpaceSurvivors.Core.StarfieldParallax"/>.
    /// Editor-only content generation — the result is a committed PNG asset, this script just
    /// (re)creates it. Menu: SpaceSurvivors/Build/Starfield Texture.
    /// </summary>
    internal static class StarfieldTextureBaker
    {
        private const string OutPath = "Assets/_Project/Art/Sprites/Generated/StarTile.png";
        private const int Size = 256;

        [MenuItem("SpaceSurvivors/Build/Starfield Texture")]
        public static void Bake()
        {
            var rng = new System.Random(20260828);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);

            var px = new Color32[Size * Size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

            // Three passes: faint dust, mid stars, a few bright ones.
            // Kept deliberately sparse — a dense field strains the eye over a whole session.
            AddStars(px, rng, count: 70, minR: 0.55f, maxR: 1.0f, minA: 0.06f, maxA: 0.16f);
            AddStars(px, rng, count: 28, minR: 0.9f,  maxR: 1.5f, minA: 0.22f, maxA: 0.45f);
            AddStars(px, rng, count: 6,  minR: 1.3f,  maxR: 2.4f, minA: 0.55f, maxA: 0.80f);

            tex.SetPixels32(px);
            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            File.WriteAllBytes(OutPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(OutPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(OutPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 100f;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Debug.Log($"[StarfieldTextureBaker] wrote {OutPath}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Sprite>(OutPath);
        }

        /// <summary>Draws soft round stars, wrapping at the edges so the texture tiles seamlessly.</summary>
        private static void AddStars(Color32[] px, System.Random rng, int count,
                                     float minR, float maxR, float minA, float maxA)
        {
            for (int s = 0; s < count; s++)
            {
                float cx = (float)rng.NextDouble() * Size;
                float cy = (float)rng.NextDouble() * Size;
                float radius = Mathf.Lerp(minR, maxR, (float)rng.NextDouble());
                float peak = Mathf.Lerp(minA, maxA, (float)rng.NextDouble());

                // slight colour variation: white → pale blue → pale amber
                float hueRoll = (float)rng.NextDouble();
                Color tint = hueRoll < 0.7f ? new Color(1f, 1f, 1f)
                           : hueRoll < 0.9f ? new Color(0.72f, 0.82f, 1f)
                                            : new Color(1f, 0.86f, 0.7f);

                int rad = Mathf.CeilToInt(radius * 2.5f) + 1;
                for (int dy = -rad; dy <= rad; dy++)
                for (int dx = -rad; dx <= rad; dx++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = peak * Mathf.Exp(-(d * d) / (2f * radius * radius));
                    if (a <= 0.003f) continue;

                    int x = ((int)cx + dx) % Size; if (x < 0) x += Size;
                    int y = ((int)cy + dy) % Size; if (y < 0) y += Size;
                    int idx = y * Size + x;

                    Color existing = px[idx];
                    float na = Mathf.Clamp01(existing.a + a);
                    Color nc = Color.Lerp(existing, tint, a / Mathf.Max(na, 0.0001f));
                    px[idx] = new Color(nc.r, nc.g, nc.b, na);
                }
            }
        }
    }
}
