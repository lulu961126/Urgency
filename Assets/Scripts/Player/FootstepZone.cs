using UnityEngine;

/// <summary>
/// 定義該區域的地板材質
/// </summary>
public class FootstepZone : MonoBehaviour
{
    public enum SurfaceType { Default, Wood, Concrete, Grass, Metal, Water }

    [Header("材質設定")]
    public SurfaceType surfaceType = SurfaceType.Default;

    private void Reset()
    {
        // 自動幫物件加上 Trigger
        var col = GetComponent<Collider2D>();
        if (col == null) 
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
    }
}
