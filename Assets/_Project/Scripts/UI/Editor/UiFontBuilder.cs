using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace FreezeOrThrow.EditorTools
{
    /// <summary>
    /// Builds the Baloo 2 TextMeshPro font assets and the label shadow materials.
    /// There is no menu item on purpose. It overwrites the assets, so run it from the command line
    /// with the editor closed:
    /// Unity -batchmode -quit -projectPath . -executeMethod FreezeOrThrow.EditorTools.UiFontBuilder.BuildAll
    /// </summary>
    public static class UiFontBuilder
    {
        const string Folder = "Assets/_Project/Art/UI/Fonts/";
        const int AtlasSize = 1024;
        const string PunctuationOutsideLatin1 = "–—‘’“”•…€";

        // Figma draws labels with a hard shadow straight down: 3 px on buttons, 6 px on the big title.
        const float LabelShadowDistance = 3f;
        const float TitleShadowDistance = 6f;
        const float FeltLabelFontSize = 44f;
        const float WoodLabelFontSize = 30f;
        const float TitleFontSize = 104f;

        /// <summary>Largest sampling size first, so glyphs are as sharp as the atlas allows.</summary>
        static readonly int[] SamplingSizes = { 80, 76, 72, 68, 64, 60, 56, 52, 48 };

        public static void BuildAll()
        {
            Build("Baloo2-Medium");
            Build("Baloo2-Bold");
            TMP_FontAsset extraBold = Build("Baloo2-ExtraBold");

            BuildShadowMaterial(extraBold, "Shadow Red", "#8E2328", FeltLabelFontSize, LabelShadowDistance);
            BuildShadowMaterial(extraBold, "Shadow Green", "#1E5A3C", FeltLabelFontSize, LabelShadowDistance);
            BuildShadowMaterial(extraBold, "Shadow Wood", "#5E3B26", WoodLabelFontSize, LabelShadowDistance);
            BuildShadowMaterial(extraBold, "Shadow Title", "#5E3B26", TitleFontSize, TitleShadowDistance);

            AssetDatabase.SaveAssets();
        }

        static TMP_FontAsset Build(string fontName)
        {
            string fontPath = Folder + fontName + ".ttf";
            string assetPath = Folder + fontName + " SDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                throw new System.IO.FileNotFoundException("Font not found", fontPath);
            }

            AssetDatabase.DeleteAsset(assetPath);
            string characters = BuildCharacterSet();

            foreach (int samplingSize in SamplingSizes)
            {
                int padding = Mathf.Max(5, samplingSize / 10);
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                    font, samplingSize, padding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize,
                    AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);

                if (!fontAsset.TryAddCharacters(characters, out string missing) || missing.Length > 0)
                {
                    Object.DestroyImmediate(fontAsset);
                    continue;
                }

                SaveAsStatic(fontAsset, fontName, assetPath);
                Debug.Log($"[UiFontBuilder] {fontName}: sampling {samplingSize}, padding {padding}, " +
                          $"{characters.Length} characters");
                return fontAsset;
            }

            throw new System.InvalidOperationException($"{fontName} does not fit in a {AtlasSize} atlas");
        }

        static void SaveAsStatic(TMP_FontAsset fontAsset, string fontName, string assetPath)
        {
            Texture2D atlas = fontAsset.atlasTextures[0];
            Material material = fontAsset.material;
            fontAsset.name = fontName + " SDF";
            atlas.name = fontName + " SDF Atlas";
            material.name = fontName + " SDF Material";

            AssetDatabase.CreateAsset(fontAsset, assetPath);
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            AssetDatabase.AddObjectToAsset(material, fontAsset);

            // Static so the atlas never rewrites itself: .asset files stay in normal git, and a
            // Dynamic atlas would show up as a huge diff every time a new character appears.
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
        }

        static void BuildShadowMaterial(
            TMP_FontAsset fontAsset, string suffix, string hexColor, float fontSize, float distance)
        {
            ColorUtility.TryParseHtmlString(hexColor, out Color color);

            // The underlay offset is in units of the SDF gradient, measured at the sampling size.
            float gradientScale = fontAsset.material.GetFloat(ShaderUtilities.ID_GradientScale);
            float samplingSize = fontAsset.faceInfo.pointSize;
            float offset = distance * samplingSize / (fontSize * gradientScale);

            var material = new Material(fontAsset.material) { name = $"{fontAsset.name} {suffix}" };
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, color);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -offset);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);

            string path = $"{Folder}{material.name}.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(material, path);
        }

        static string BuildCharacterSet()
        {
            var characters = new System.Text.StringBuilder();
            AppendRange(characters, 0x20, 0x7E);
            AppendRange(characters, 0xA1, 0xFF);
            characters.Append(PunctuationOutsideLatin1);
            return characters.ToString();
        }

        static void AppendRange(System.Text.StringBuilder characters, int first, int last)
        {
            for (int unicode = first; unicode <= last; unicode++)
            {
                characters.Append((char)unicode);
            }
        }
    }
}
