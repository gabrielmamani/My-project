using System.Collections.Generic;
using UnityEngine;
using Gunbound.Network;

namespace Gunbound.Environment
{
    /// <summary>
    /// Component responsible for loading dynamic 2D background, atmosphere color, 
    /// and destructible terrain texture based on active MapData selection (RF-5.4.1, RF-5.4.2).
    /// </summary>
    public class MapEnvironmentLoader : MonoBehaviour
    {
        public static MapEnvironmentLoader Instance { get; private set; }

        [Header("Available Map Data Assets")]
        [SerializeField] private List<MapData> _availableMaps = new List<MapData>();

        [Header("Scene Object References")]
        [SerializeField] private DestructibleTerrain _terrain;
        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private Camera _mainCamera;

        [Header("Active State")]
        [SerializeField] private MapType _currentMapType = MapType.MiramoTown;

        public MapType CurrentMapType => _currentMapType;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindReferencesIfMissing();

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnMapSelectionChanged += HandleMapSelectionChanged;
                LoadMap(NetworkGameManager.Instance.SelectedMap);
            }
            else
            {
                LoadMap(_currentMapType);
            }
        }

        private void OnDestroy()
        {
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnMapSelectionChanged -= HandleMapSelectionChanged;
            }
        }

        private void FindReferencesIfMissing()
        {
            if (_terrain == null) _terrain = FindAnyObjectByType<DestructibleTerrain>();
            if (_mainCamera == null) _mainCamera = Camera.main;

            if (_backgroundRenderer == null)
            {
                GameObject bgObj = GameObject.Find("Background") ?? GameObject.Find("BG_Parallax");
                if (bgObj != null)
                {
                    _backgroundRenderer = bgObj.GetComponent<SpriteRenderer>();
                }
            }
        }

        public MapData GetMapData(MapType type)
        {
            foreach (var map in _availableMaps)
            {
                if (map != null && map.MapType == type) return map;
            }
            return null;
        }

        private void HandleMapSelectionChanged(MapType mapType)
        {
            LoadMap(mapType);
        }

        public void LoadMap(MapType mapType)
        {
            _currentMapType = mapType;
            MapData data = GetMapData(mapType);

            if (data == null)
            {
                Debug.LogWarning($"[MapEnvironmentLoader] MapData asset not found for MapType '{mapType}'!");
                return;
            }

            FindReferencesIfMissing();

            // 1. Update Destructible Terrain
            if (_terrain != null)
            {
                _terrain.ApplyMapData(data);
            }

            // 2. Update Background Sprite
            if (_backgroundRenderer != null && data.BackgroundSprite != null)
            {
                _backgroundRenderer.sprite = data.BackgroundSprite;
            }

            // 3. Update Camera Atmosphere Color
            if (_mainCamera != null)
            {
                _mainCamera.backgroundColor = data.AtmosphereColor;
            }

            // 4. Update Physics Gravity
            Physics2D.gravity = new Vector2(0f, -9.81f * data.GravityMultiplier);

            Debug.Log($"[MapEnvironmentLoader] Successfully loaded Map '{data.MapName}' (WindMultiplier={data.WindMultiplier}x, GravityMultiplier={data.GravityMultiplier}x).");
        }
    }
}
