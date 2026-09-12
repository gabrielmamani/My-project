using UnityEngine;

namespace Gunbound.Environment
{
    /// <summary>
    /// ScriptableObject defining 2.5D Map Environment parameters, textures, and physics attributes (RF-5.4.1, RF-5.4.2).
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapData", menuName = "Gunbound/Map Data", order = 2)]
    public class MapData : ScriptableObject
    {
        [Header("Map Identity")]
        [SerializeField] private string _mapName = "Miramo Town";
        [SerializeField] private MapType _mapType = MapType.MiramoTown;
        [SerializeField, TextArea(2, 4)] private string _description = "Pradera verde clásica con clima templado.";

        [Header("Visual & Texture Assets")]
        [SerializeField] private Sprite _groundSprite;
        [SerializeField] private Sprite _backgroundSprite;
        [SerializeField] private Sprite _mapPreviewIcon;
        [SerializeField] private Color _atmosphereColor = new Color(0.4f, 0.7f, 1.0f, 1.0f);

        [Header("Environmental Modifiers")]
        [SerializeField] private float _windMultiplier = 1.0f;
        [SerializeField] private float _gravityMultiplier = 1.0f;

        public string MapName => _mapName;
        public MapType MapType => _mapType;
        public string Description => _description;
        public Sprite GroundSprite => _groundSprite;
        public Sprite BackgroundSprite => _backgroundSprite;
        public Sprite MapPreviewIcon => _mapPreviewIcon;
        public Color AtmosphereColor => _atmosphereColor;
        public float WindMultiplier => _windMultiplier;
        public float GravityMultiplier => _gravityMultiplier;

        public void SetSprites(Sprite ground, Sprite bg, Sprite preview = null)
        {
            _groundSprite = ground;
            _backgroundSprite = bg;
            if (preview != null) _mapPreviewIcon = preview;
        }
    }
}
