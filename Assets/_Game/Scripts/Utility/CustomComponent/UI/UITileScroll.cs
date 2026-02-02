using UnityEngine;
using UnityEngine.UI;

public class UITileScroll : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.05f, 0.05f);

    public RawImage raw;
    public Rect uv;

    void Awake()
    {
        uv = raw.uvRect;
    }

    void Update()
    {
        uv.x += speed.x * Time.deltaTime;
        uv.y += speed.y * Time.deltaTime;
        raw.uvRect = uv;
    }
}