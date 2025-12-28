using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HurtZone : MonoBehaviour
{
    [Tooltip("每秒扣血量（只對 Player 生效）")]
    public float damagePerSecond = 10f;

    [Tooltip("玩家的 Tag")]
    public string playerTag = "Player";

    void Reset()
    {
        // 確保是 Trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // 只對玩家扣血
        Informations.PlayerGetDamage(damagePerSecond * Time.deltaTime, false);
    }
}