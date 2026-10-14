using UnityEditor;
using UnityEngine;

/// <summary>
/// Configures import settings and grid slices for pixel-art sprites in Assets/Sprites.
/// Sets Point filtering, uncompressed format, PPU = 1, and correct slices for:
/// - Hammer_sheet.png: 64x64 grid (2 frames, top = Hammer_0, bottom = Hammer_1)
/// - Nails_sheet.png: 32x32 grid (2x9 layout, top row comments excluded, bottom row = Nail_0..Nail_8)
/// - Board_block.png: 64x32 single tile
/// </summary>
public static class SpriteImportSetup
{
    [MenuItem("Tools/Setup Sprite Assets")]
    public static void SetupAllSprites()
    {
        SetupHammerSheet();
        SetupNailsSheet();
        SetupBoardBlock();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Sprite import settings and slices successfully configured!");
    }

    private static void SetupHammerSheet()
    {
        string path = "Assets/Sprites/Hammer_sheet.png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null)
        {
            Debug.LogError($"Could not load TextureImporter at {path}");
            return;
        }

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.spritePixelsToUnits = 1;

        // Texture is 64 x 128.
        // In Unity coordinates (origin bottom-left):
        // Top frame (Hammer_0, hammer up/ready): y = 64..128
        // Bottom frame (Hammer_1, hammer strike/hit): y = 0..64
        var metas = new SpriteMetaData[2];
        metas[0] = new SpriteMetaData
        {
            name = "Hammer_0",
            rect = new Rect(0, 64, 64, 64),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f)
        };
        metas[1] = new SpriteMetaData
        {
            name = "Hammer_1",
            rect = new Rect(0, 0, 64, 64),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f)
        };

        ti.spritesheet = metas;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
    }

    private static void SetupNailsSheet()
    {
        string path = "Assets/Sprites/Nails_sheet.png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null)
        {
            Debug.LogError($"Could not load TextureImporter at {path}");
            return;
        }

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.spritePixelsToUnits = 1;

        // Texture is 288 x 64 (2 rows x 9 columns of 32x32 tiles).
        // First row (top in image = y:32..64 in Unity) is notes/comments (excluded).
        // Second row (bottom in image = y:0..32 in Unity) contains 9 nail sprites (Nail_0 .. Nail_8).
        var metas = new SpriteMetaData[9];
        for (int i = 0; i < 9; i++)
        {
            metas[i] = new SpriteMetaData
            {
                name = $"Nail_{i}",
                rect = new Rect(i * 32, 0, 32, 32),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }

        ti.spritesheet = metas;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
    }

    private static void SetupBoardBlock()
    {
        string path = "Assets/Sprites/Board_block.png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null)
        {
            Debug.LogError($"Could not load TextureImporter at {path}");
            return;
        }

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.spritePixelsToUnits = 1;

        // Texture is 64 x 32 (1 single tile).
        var metas = new SpriteMetaData[1];
        metas[0] = new SpriteMetaData
        {
            name = "Board_block_0",
            rect = new Rect(0, 0, 64, 32),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f)
        };

        ti.spritesheet = metas;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
    }
}
