using UnityEngine;
using System;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Handles all player-related functionality
    /// </summary>
    public interface IPlayerSystem
    {
        float Health { get; }
        float MaxHealth { get; }
        int XP { get; }
        Vector3 Position { get; }
        float Speed { get; }
        float AttackDamage { get; }
        float AttackRange { get; }
        float AttackRate { get; }
        void TakeDamage(float damage);
        void Heal(float amount);
        void AddXP(int amount);
        void DeductXP(int amount);
        void ApplySkillEffect(ISkillEffect effect);
        void Update();
        Vector3 GetMovementInput();
        bool ShouldAttack(out float attackDamage, out float attackRange);
        void OnAttackPerformed(); // Missing method added
    }
}