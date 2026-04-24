using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class AutoTile : MonoBehaviour
{
    public float textureSize = 1f;

    private Renderer rend;
    private MaterialPropertyBlock propBlock;

    void Update()
    {
        if (transform.hasChanged)
        {
            UpdateTiling();
            transform.hasChanged = false;
        }
    }

    void UpdateTiling()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        rend.GetPropertyBlock(propBlock);
        Vector2 tiling = new Vector2(transform.localScale.x / textureSize, transform.localScale.y / textureSize);
        propBlock.SetVector("_BaseMap_ST", new Vector4(tiling.x, tiling.y, 0, 0));
        rend.SetPropertyBlock(propBlock);
    }
}