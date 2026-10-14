using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton manager for reusable pixel-art sprites.
/// Caches and provides access to:
/// - Hammer sprites (HammerUp / HammerHit, 64x64)
/// - Nail sprites (Nail_0 .. Nail_8, 32x32)
/// - Board block sprite (Board_block_0, 64x32)
/// Provides helpers for 360x640 grid alignment and integer scaling.
/// </summary>
[DefaultExecutionOrder(-50)]
public class GameSpriteManager : MonoBehaviour
{
    public const int GridWidth = 360;
    public const int GridHeight = 640;

    public static GameSpriteManager Instance { get; private set; }

    [Header("Hammer Sprites (64x64)")]
    [SerializeField] private Sprite _hammerUp;
    [SerializeField] private Sprite _hammerHit;
    [SerializeField] private Sprite[] _hammerSprites = new Sprite[2];

    [Header("Nail Sprites (32x32, 9 variants)")]
    [SerializeField] private Sprite[] _nailSprites = new Sprite[9];

    [Header("Board Block Sprite (64x32)")]
    [SerializeField] private Sprite _boardBlock;

    // Public properties
    public Sprite HammerUp => _hammerUp;
    public Sprite HammerHit => _hammerHit;
    public Sprite[] HammerSprites => _hammerSprites;
    public Sprite[] NailSprites => _nailSprites;
    public Sprite BoardBlock => _boardBlock;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            var existing = FindAnyObjectByType<GameSpriteManager>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var prefab = Resources.Load<GameObject>("GameSpriteManager");
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                go.name = "[GameSpriteManager]";
                DontDestroyOnLoad(go);
            }
            else
            {
                var go = new GameObject("[GameSpriteManager]");
                var mgr = go.AddComponent<GameSpriteManager>();
                DontDestroyOnLoad(go);
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSpritesLoaded();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void EnsureSpritesLoaded()
    {
#if UNITY_EDITOR
        if (_hammerUp == null || _hammerHit == null || _nailSprites == null || _nailSprites.Length == 0 || _boardBlock == null)
        {
            LoadEditorAssets();
        }
#endif
    }

#if UNITY_EDITOR
    public void LoadEditorAssets()
    {
        // Load Hammer sprites
        var hammerAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Hammer_sheet.png")
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (hammerAssets.Length >= 2)
        {
            _hammerUp = hammerAssets.FirstOrDefault(s => s.name == "Hammer_0") ?? hammerAssets[0];
            _hammerHit = hammerAssets.FirstOrDefault(s => s.name == "Hammer_1") ?? hammerAssets[1];
            _hammerSprites = new Sprite[] { _hammerUp, _hammerHit };
        }

        // Load Nail sprites
        var nailAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Nails_sheet.png")
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        _nailSprites = new Sprite[9];
        for (int i = 0; i < 9; i++)
        {
            string targetName = $"Nail_{i}";
            _nailSprites[i] = nailAssets.FirstOrDefault(s => s.name == targetName) ?? (i < nailAssets.Length ? nailAssets[i] : null);
        }

        // Load Board block sprite
        var boardAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Board_block.png")
            .OfType<Sprite>()
            .ToArray();

        if (boardAssets.Length > 0)
        {
            _boardBlock = boardAssets[0];
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void OnValidate()
    {
        EnsureSpritesLoaded();
    }
#endif

    /// <summary>
    /// Returns the hammer sprite for the specified state (true = hit/strike, false = up/ready).
    /// </summary>
    public Sprite GetHammerSprite(bool isHit)
    {
        return isHit ? _hammerHit : _hammerUp;
    }

    /// <summary>
    /// Returns the hammer sprite for the specified frame index (0 = up, 1 = hit).
    /// </summary>
    public Sprite GetHammerSprite(int frameIndex)
    {
        if (_hammerSprites != null && _hammerSprites.Length > 0)
        {
            int clamped = Mathf.Clamp(frameIndex, 0, _hammerSprites.Length - 1);
            return _hammerSprites[clamped];
        }
        return frameIndex == 1 ? _hammerHit : _hammerUp;
    }

    /// <summary>
    /// Returns the nail sprite for the specified index (0 to 8).
    /// </summary>
    public Sprite GetNailSprite(int index)
    {
        if (_nailSprites == null || _nailSprites.Length == 0) return null;
        int clamped = Mathf.Clamp(index, 0, _nailSprites.Length - 1);
        return _nailSprites[clamped];
    }

    /// <summary>
    /// Returns the single board block tile sprite.
    /// </summary>
    public Sprite GetBoardSprite()
    {
        return _boardBlock;
    }

    /// <summary>
    /// Snaps a position to the nearest integer pixel on the 360x640 grid.
    /// </summary>
    public static Vector2 SnapToGrid(Vector2 pos)
    {
        return new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
    }

    /// <summary>
    /// Snaps a position to the nearest integer pixel on the 360x640 grid.
    /// </summary>
    public static Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
    }

    /// <summary>
    /// Creates and configures a SpriteRenderer GameObject aligned to the 360x640 grid with integer scaling.
    /// </summary>
    public GameObject CreateSpriteObject(string name, Sprite sprite, Vector3 gridPosition, int integerScale = 1, int sortingOrder = 0, Transform parent = null)
    {
        int scale = Mathf.Max(1, integerScale);
        var go = new GameObject(name);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        go.transform.localPosition = SnapToGrid(gridPosition);
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        return go;
    }

    /// <summary>
    /// Spawns a board block tile on the grid.
    /// </summary>
    public GameObject SpawnBoardBlock(Vector3 gridPosition, int integerScale = 1, int sortingOrder = 0, Transform parent = null)
    {
        return CreateSpriteObject("BoardBlock", _boardBlock, gridPosition, integerScale, sortingOrder, parent);
    }

    /// <summary>
    /// Spawns a nail sprite on the grid.
    /// </summary>
    public GameObject SpawnNail(int nailIndex, Vector3 gridPosition, int integerScale = 1, int sortingOrder = 1, Transform parent = null)
    {
        return CreateSpriteObject($"Nail_{nailIndex}", GetNailSprite(nailIndex), gridPosition, integerScale, sortingOrder, parent);
    }

    /// <summary>
    /// Spawns a hammer sprite on the grid.
    /// </summary>
    public GameObject SpawnHammer(bool isHit, Vector3 gridPosition, int integerScale = 1, int sortingOrder = 2, Transform parent = null)
    {
        return CreateSpriteObject("Hammer", GetHammerSprite(isHit), gridPosition, integerScale, sortingOrder, parent);
    }
}
