using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FreezeOrThrow.EditorTools
{
    /// <summary>
    /// Applies the UI sprite settings (README section 9) the first time a PNG lands in Art/UI/.
    /// It only runs when an image has no .meta yet, so anything set by hand in the Sprite Editor
    /// afterwards is kept, and re-exporting a PNG over an existing one keeps its settings.
    /// </summary>
    public class UiSpriteImporter : AssetPostprocessor
    {
        const string UiArtFolder = "Assets/_Project/Art/UI/";

        // Figma exports are @2x (icons @4x), so these values make 1 Figma pixel = 1 canvas unit
        // at a Canvas Reference Pixels Per Unit of 100. Set Native Size gives the Figma size.
        const float PpuSprite = 200f;
        const float PpuIcon = 400f;
        const float PpuOneX = 100f;

        // 9-slice borders in PNG pixels, as (left, bottom, right, top). Each value is the shadow
        // padding inside the PNG plus the corner that must not stretch, so it is measured on the
        // sprites as they are after Tools/normalize_ui_sprites.py.
        static readonly Dictionary<string, Vector4> Borders = new()
        {
            { "btn_felt_red_default", new Vector4(124, 164, 124, 100) },
            { "btn_felt_red_hover", new Vector4(124, 164, 124, 100) },
            { "btn_felt_red_pressed", new Vector4(124, 164, 124, 100) },
            { "btn_felt_green_default", new Vector4(124, 164, 124, 100) },
            { "btn_felt_green_hover", new Vector4(124, 164, 124, 100) },
            { "btn_felt_green_pressed", new Vector4(124, 164, 124, 100) },
            { "btn_wood_default", new Vector4(88, 120, 88, 72) },
            { "btn_wood_hover", new Vector4(88, 120, 88, 72) },
            { "btn_wood_pressed", new Vector4(88, 120, 88, 72) },
            { "btn_pill_default", new Vector4(84, 84, 84, 84) },
            { "btn_pill_hover", new Vector4(84, 84, 84, 84) },
            { "btn_pill_pressed", new Vector4(84, 84, 84, 84) },
            { "sprite_sign_wood", new Vector4(120, 156, 120, 84) },
            { "sprite_panel_cream", new Vector4(160, 160, 160, 112) },
            { "sprite_code_box_empty", new Vector4(72, 72, 72, 72) },
            { "sprite_code_box_focused", new Vector4(72, 72, 72, 72) },
            { "sprite_code_box_filled", new Vector4(72, 72, 72, 72) },
            { "sprite_lobby_card_red", new Vector4(160, 208, 160, 112) },
            { "sprite_lobby_card_blue", new Vector4(160, 208, 160, 112) },
            { "sprite_lobby_card_green", new Vector4(160, 208, 160, 112) },
            { "sprite_lobby_card_yellow", new Vector4(160, 208, 160, 112) },
            { "sprite_score_tile_red", new Vector4(104, 132, 104, 76) },
            { "sprite_score_tile_blue", new Vector4(96, 124, 96, 68) },
            { "sprite_score_tile_green", new Vector4(96, 124, 96, 68) },
            { "sprite_score_tile_yellow", new Vector4(96, 124, 96, 68) },
            { "sprite_event_banner", new Vector4(128, 128, 128, 80) },
            { "sprite_crate_1st", new Vector4(140, 188, 140, 92) },
            { "sprite_crate_2nd", new Vector4(140, 188, 140, 92) },
            { "sprite_crate_3rd", new Vector4(140, 188, 140, 92) },
        };

        // The glows are almost fully transparent, and compression shows up on them as a speckled
        // ring, so they stay uncompressed.
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(UiArtFolder) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            string spriteName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnitFor(spriteName);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = spriteName.StartsWith("sprite_glow_")
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = spriteName == "bg_cabin" ? 8192 : 4096;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            if (Borders.TryGetValue(spriteName, out Vector4 border))
            {
                settings.spriteBorder = border;
            }

            importer.SetTextureSettings(settings);
        }

        static float PixelsPerUnitFor(string spriteName)
        {
            if (spriteName.StartsWith("icon_"))
            {
                return PpuIcon;
            }

            return spriteName == "sprite_light_rays" ? PpuOneX : PpuSprite;
        }
    }
}
