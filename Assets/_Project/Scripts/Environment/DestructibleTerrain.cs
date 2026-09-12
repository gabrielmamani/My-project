using UnityEngine;

namespace Gunbound.Environment
{
    /// <summary>
    /// Manages dynamic 2D texture-based destructible terrain (Bunge mechanic).
    /// Clones ground texture into a readable RGBA32 format at runtime, carves circular transparent holes
    /// upon impact, and updates PolygonCollider2D physics geometry dynamically.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DestructibleTerrain : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private Texture2D _texture;
        private PolygonCollider2D _polygonCollider;

        private Vector2 _spriteMinBounds;
        private Vector2 _spriteMaxBounds;

        private void Awake()
        {
            InitializeTerrainTexture();
        }

        /// <summary>
        /// Dynamically reconfigures terrain base texture and collider from a MapData asset (RF-5.4.2).
        /// </summary>
        public void ApplyMapData(MapData mapData)
        {
            if (mapData == null || mapData.GroundSprite == null) return;

            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = mapData.GroundSprite;
                InitializeTerrainTexture();
                Debug.Log($"[DestructibleTerrain] Applied MapData '{mapData.MapName}' to ground terrain.");
            }
        }

        /// <summary>
        /// Initializes editable RGBA32 texture and assigns PolygonCollider2D.
        /// </summary>
        public void InitializeTerrainTexture()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null || _spriteRenderer.sprite == null)
            {
                Debug.LogError("[DestructibleTerrain] SpriteRenderer or Sprite missing on ground object!");
                return;
            }

            Sprite sourceSprite = _spriteRenderer.sprite;
            Texture2D sourceTex = sourceSprite.texture;

            // Save unscaled local bounds
            _spriteMinBounds = sourceSprite.bounds.min;
            _spriteMaxBounds = sourceSprite.bounds.max;

            // Safely copy sourceTex into a new RGBA32 Texture2D using RenderTexture (handles non-readable textures)
            RenderTexture rt = RenderTexture.GetTemporary(
                sourceTex.width, sourceTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(sourceTex, rt);

            RenderTexture prevRT = RenderTexture.active;
            RenderTexture.active = rt;

            _texture = new Texture2D(sourceTex.width, sourceTex.height, TextureFormat.RGBA32, false);
            _texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            _texture.Apply();

            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(rt);

            // Re-create sprite from cloned texture
            Vector2 pivotRatio = new Vector2(
                sourceSprite.pivot.x / sourceTex.width,
                sourceSprite.pivot.y / sourceTex.height);

            Sprite editableSprite = Sprite.Create(
                _texture,
                new Rect(0, 0, _texture.width, _texture.height),
                pivotRatio,
                sourceSprite.pixelsPerUnit,
                0,
                SpriteMeshType.Tight,
                Vector4.zero,
                true);

            _spriteRenderer.sprite = editableSprite;

            // Ensure PolygonCollider2D is configured
            RebuildCollider();
        }

        /// <summary>
        /// Carves a transparent circular hole in the terrain texture at the specified world position and updates physics.
        /// </summary>
        /// <param name="worldPosition">World coordinates of crater center.</param>
        /// <param name="radius">Radius of hole in world units.</param>
        public void CarveHole(Vector2 worldPosition, float radius)
        {
            if (_texture == null || _spriteRenderer == null || _spriteRenderer.sprite == null) return;

            Sprite sprite = _spriteRenderer.sprite;
            Vector3 localPos3D = transform.InverseTransformPoint(worldPosition);
            Vector2 localPos = new Vector2(localPos3D.x, localPos3D.y);

            // Calculate world scale of terrain
            float worldWidth = transform.lossyScale.x * sprite.bounds.size.x;
            float worldHeight = transform.lossyScale.y * sprite.bounds.size.y;

            if (worldWidth <= 0f || worldHeight <= 0f) return;

            // Center pixel coordinates
            float centerNormX = Mathf.InverseLerp(_spriteMinBounds.x, _spriteMaxBounds.x, localPos.x);
            float centerNormY = Mathf.InverseLerp(_spriteMinBounds.y, _spriteMaxBounds.y, localPos.y);

            float centerPixelX = centerNormX * _texture.width;
            float centerPixelY = centerNormY * _texture.height;

            // Bounding box in pixel units
            float pixelRadiusX = radius * (_texture.width / worldWidth);
            float pixelRadiusY = radius * (_texture.height / worldHeight);

            int minX = Mathf.Clamp(Mathf.FloorToInt(centerPixelX - pixelRadiusX - 1), 0, _texture.width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(centerPixelX + pixelRadiusX + 1), 0, _texture.width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(centerPixelY - pixelRadiusY - 1), 0, _texture.height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(centerPixelY + pixelRadiusY + 1), 0, _texture.height - 1);

            Color32[] pixels = _texture.GetPixels32();
            bool modified = false;

            for (int y = minY; y <= maxY; y++)
            {
                float dy = (y + 0.5f - centerPixelY) / pixelRadiusY;
                float dySq = dy * dy;
                if (dySq > 1f) continue;

                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x + 0.5f - centerPixelX) / pixelRadiusX;
                    if (dx * dx + dySq <= 1f)
                    {
                        int index = y * _texture.width + x;
                        if (pixels[index].a != 0)
                        {
                            pixels[index] = new Color32(0, 0, 0, 0);
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
            {
                _texture.SetPixels32(pixels);
                _texture.Apply();

                // Re-create sprite and physics shape
                Vector2 pivotRatio = new Vector2(
                    sprite.pivot.x / _texture.width,
                    sprite.pivot.y / _texture.height);

                _spriteRenderer.sprite = Sprite.Create(
                    _texture,
                    new Rect(0, 0, _texture.width, _texture.height),
                    pivotRatio,
                    sprite.pixelsPerUnit,
                    0,
                    SpriteMeshType.Tight,
                    Vector4.zero,
                    true);

                RebuildCollider();
                WakeUpPhysicsObjects();
                Debug.Log($"[DestructibleTerrain] Carved hole at {worldPosition} (Radius: {radius}m)");
            }
        }

        private void WakeUpPhysicsObjects()
        {
            var rigidbodies = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
            foreach (var rb in rigidbodies)
            {
                if (rb != null)
                {
                    rb.WakeUp();
                }
            }
        }

        private void RebuildCollider()
        {
            var boxCol = GetComponent<BoxCollider2D>();
            if (boxCol != null)
            {
                DestroyImmediate(boxCol);
            }

            var polyCol = GetComponent<PolygonCollider2D>();
            if (polyCol != null)
            {
                DestroyImmediate(polyCol);
            }

            _polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        }
    }
}
