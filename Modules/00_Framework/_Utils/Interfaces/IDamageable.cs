using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage, BattleUnit attacker = null, bool isCritical = false);
    Transform GetDamagePoint();
    bool IsAlive { get; }
    void Heal(int amount);
    void Die();
}
