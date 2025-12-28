using UnityEngine;

/// <summary>
/// 傷害接收介面 - 讓武器不再需要知道目標具體的類別 (Zombie, RangedZombie 等)
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 接收傷害的方法
    /// </summary>
    /// <param name="damage">傷害數值</param>
    /// <param name="knockbackDistance">擊退距離</param>
    /// <param name="knockbackVelocity">擊退速度</param>
    void TakeDamage(float damage, float knockbackDistance, float knockbackVelocity, Vector2 sourcePosition);
    
    /// <summary>
    /// 取得該物件所在的 GameObject
    /// </summary>
    GameObject GetGameObject();
}
