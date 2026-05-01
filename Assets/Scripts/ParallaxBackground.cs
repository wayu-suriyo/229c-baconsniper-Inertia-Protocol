using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        [Tooltip("The original sprite GameObject (must have a SpriteRenderer).")]
        public Transform layerTransform;

        [Tooltip("0 = static (sky), 1 = moves with camera (ground), >1 = foreground.")]
        [Range(-0.5f, 2f)]
        public float parallaxFactor = 0.5f;

        [Tooltip("Enables automatic horizontal infinite scrolling for this layer.")]
        public bool infiniteScrolling = true;

        [Tooltip("Extra gap (in world units) between each repeated tile. 0 = seamless. Negative = overlap.")]
        public float tileSpacing = 0f;

        [Tooltip("Enable vertical parallax as well (usually only for very deep layers like sky).")]
        public bool parallaxY = false;

        // Runtime state — not shown in Inspector
        [HideInInspector] public Transform[] tiles;
        [HideInInspector] public float tileWidth;
        [HideInInspector] public float startPosX;
        [HideInInspector] public float startPosY;
    }

    [Header("Camera Reference")]
    [Tooltip("Leave empty to auto-find Camera.main.")]
    public Transform cameraTransform;

    [Header("Layers")]
    public ParallaxLayer[] layers;

    [Header("Tile Count")]
    [Tooltip("How many tiles per side of the original. 1 = 3 total (left, center, right). 2 = 5 total. Higher values cover ultra-wide views.")]
    [Range(1, 4)]
    public int tilesPerSide = 1;

    void Start()
    {
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        foreach (var layer in layers)
        {
            if (layer.layerTransform == null) continue;

            SpriteRenderer sr = layer.layerTransform.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogWarning($"ParallaxBackground: '{layer.layerTransform.name}' has no SpriteRenderer. Skipping.");
                continue;
            }

            // Calculate the effective tile width (sprite bounds + user spacing)
            layer.tileWidth = sr.bounds.size.x + layer.tileSpacing;
            layer.startPosX = layer.layerTransform.position.x;
            layer.startPosY = layer.layerTransform.position.y;

            if (layer.infiniteScrolling)
            {
                CreateTiles(layer, sr);
            }
            else
            {
                // Just store the original as a single "tile"
                layer.tiles = new Transform[] { layer.layerTransform };
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        float camX = cameraTransform.position.x;
        float camY = cameraTransform.position.y;

        foreach (var layer in layers)
        {
            if (layer.layerTransform == null || layer.tiles == null) continue;

            float yPos = layer.parallaxY
                ? layer.startPosY + camY * layer.parallaxFactor
                : layer.startPosY;

            if (layer.infiniteScrolling)
            {
                // Where the layer's origin sits due to parallax
                float anchorX = layer.startPosX + camX * layer.parallaxFactor;

                // How far the camera has drifted from the anchor
                float dist = camX - anchorX;

                // Which virtual tile in the infinite grid is closest to the camera?
                // Mathf.Round ensures we always snap to the nearest tile center.
                float nearestIndex = Mathf.Round(dist / layer.tileWidth);

                // Place all tiles centered around that index
                for (int i = 0; i < layer.tiles.Length; i++)
                {
                    if (layer.tiles[i] == null) continue;
                    int offset = i - tilesPerSide;
                    float tileX = anchorX + (nearestIndex + offset) * layer.tileWidth;

                    layer.tiles[i].position = new Vector3(
                        tileX,
                        yPos,
                        layer.tiles[i].position.z
                    );
                }
            }
            else
            {
                // Non-scrolling: simple parallax offset
                float newX = layer.startPosX + camX * layer.parallaxFactor;
                layer.tiles[0].position = new Vector3(
                    newX,
                    yPos,
                    layer.tiles[0].position.z
                );
            }
        }
    }

    /// <summary>
    /// Creates clone tiles to the left and right of the original sprite.
    /// Total tiles = 1 (original) + 2 * tilesPerSide.
    /// </summary>
    private void CreateTiles(ParallaxLayer layer, SpriteRenderer originalSR)
    {
        int totalTiles = 1 + tilesPerSide * 2;
        layer.tiles = new Transform[totalTiles];

        // Center tile is the original
        int centerIndex = tilesPerSide;
        layer.tiles[centerIndex] = layer.layerTransform;

        for (int i = 0; i < totalTiles; i++)
        {
            if (i == centerIndex) continue;

            int offset = i - centerIndex;
            GameObject clone = Instantiate(layer.layerTransform.gameObject, layer.layerTransform.parent);
            clone.name = $"{layer.layerTransform.name}_Tile_{offset:+#;-#}";

            // Position relative to original
            Vector3 pos = layer.layerTransform.position;
            pos.x += offset * layer.tileWidth;
            clone.transform.position = pos;

            layer.tiles[i] = clone.transform;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (layers == null) return;

        foreach (var layer in layers)
        {
            if (layer.layerTransform == null) continue;

            SpriteRenderer sr = layer.layerTransform.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            float w = sr.bounds.size.x + layer.tileSpacing;
            Vector3 center = layer.layerTransform.position;
            float h = sr.bounds.size.y;

            // Draw the original tile
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireCube(center, new Vector3(w, h, 0f));

            // Draw clone positions
            if (layer.infiniteScrolling)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
                for (int i = 1; i <= tilesPerSide; i++)
                {
                    Gizmos.DrawWireCube(center + Vector3.right * w * i, new Vector3(w, h, 0f));
                    Gizmos.DrawWireCube(center + Vector3.left  * w * i, new Vector3(w, h, 0f));
                }
            }

            // Label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                center + Vector3.up * h * 0.55f,
                $"{layer.layerTransform.name}  (×{layer.parallaxFactor:F2})"
            );
            #endif
        }
    }
}
