using UnityEngine;
using System.Collections.Generic;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Configuration data for player system
    /// </summary>
    [System.Serializable]
    public class PlayerConfig
    {
        public float MaxHealth { get; set; } = 100f;
        public float Speed { get; set; } = 5f;
        public float AttackDamage { get; set; } = 25f;
        public float AttackRange { get; set; } = 3f;
        public float AttackRate { get; set; } = 1f; // Attacks per second
        public Transform Transform { get; set; }
        public float DamageReduction { get; set; } = 0f;
        public float XPMultiplier { get; set; } = 1f;
    }

    /// <summary>
    /// PlayerSystem manages player state, health, XP, and skill effects
    /// </summary>
    public class PlayerSystem : IPlayerSystem
    {
        private readonly PlayerConfig _config;
        private readonly IEventBus _eventBus;
        
        // Core Stats
        private float _currentHealth;
        private float _previousHealth;
        private int _currentXP;
        private int _previousXP;
        private float _attackTimer;
        
        // Skill Modifiers
        private readonly PlayerModifiers _modifiers = new PlayerModifiers();
        
        // Active Effects Tracking
        private readonly List<ISkillEffect> _activeEffects = new List<ISkillEffect>();
        private readonly Dictionary<string, float> _statModifiers = new Dictionary<string, float>();
        
        // Combat State
        private float _lastAttackTime;
        private Vector3 _lastKnownPosition;
        
        // Properties (Interface Implementation)
        public float Health => _currentHealth;
        public float MaxHealth => _config.MaxHealth + _modifiers.HealthModifier;
        public int XP => _currentXP;
        public Vector3 Position => _config.Transform?.position ?? _lastKnownPosition;
        public float Speed => _config.Speed * _modifiers.SpeedMultiplier;
        public float AttackDamage => _config.AttackDamage + _modifiers.DamageModifier;
        public float AttackRange => _config.AttackRange + _modifiers.RangeModifier;
        public float AttackRate => _config.AttackRate * _modifiers.AttackSpeedMultiplier;
        
        // Additional Properties
        public float DamageReduction => Mathf.Clamp01(_modifiers.DamageReduction);
        public float XPMultiplier => _modifiers.XPMultiplier;
        public int ActiveEffectCount => _activeEffects.Count;
        public bool IsAlive => _currentHealth > 0;
        public bool IsCriticalHealth => Health / MaxHealth <= 0.25f;

        public PlayerSystem(PlayerConfig config, IEventBus eventBus)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
            _eventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
            
            // Initialize health to max
            _currentHealth = MaxHealth;
            _previousHealth = _currentHealth;
            _currentXP = 0;
            _previousXP = 0;
            
            // Subscribe to relevant events for XP gain
            _eventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            
            // Publish initial state
            PublishHealthChanged();
            PublishXPChanged("Initial");
            
            Debug.Log($"PlayerSystem initialized - Health: {MaxHealth}, Speed: {Speed}, Attack: {AttackDamage}");
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive || damage <= 0) return;
            
            // Apply damage reduction
            float actualDamage = damage * (1f - DamageReduction);
            actualDamage = Mathf.Max(0, actualDamage);
            
            _previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);
            
            // Publish damage event
            _eventBus.Publish(new PlayerDamagedEvent
            {
                Damage = damage,
                ActualDamage = actualDamage,
                DamageSource = Vector3.zero, 
                DamageType = "Enemy Attack",
                DamageReduction = DamageReduction
            });
            
            PublishHealthChanged();
            
            // Check for death
            if (!IsAlive)
            {
                HandlePlayerDeath();
            }
            
            Debug.Log($"Player took {actualDamage:F1} damage ({damage:F1} reduced by {DamageReduction*100:F0}%). Health: {_currentHealth:F1}/{MaxHealth:F1}");
        }

        public void OnAttackPerformed()
        {
            _attackTimer = 0f;
            _lastAttackTime = Time.time;
    
            _eventBus.Publish(new PlayerAttackEvent
            {
                AttackPosition = Position,
                Damage = AttackDamage,
                Range = AttackRange,
                AttackType = "Basic Attack"
            });
        }
        
        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0) return;
            
            _previousHealth = _currentHealth;
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
            float actualHealing = _currentHealth - oldHealth;
            
            if (actualHealing > 0)
            {
                PublishHealthChanged();
                Debug.Log($"Player healed for {actualHealing:F1}. Health: {_currentHealth:F1}/{MaxHealth:F1}");
            }
        }

        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            
            _previousXP = _currentXP;
            int modifiedAmount = Mathf.RoundToInt(amount * XPMultiplier);
            _currentXP += modifiedAmount;
            
            PublishXPChanged("Enemy Defeat");
            
            Debug.Log($"Player gained {modifiedAmount} XP (base: {amount}, multiplier: {XPMultiplier:F2}). Total XP: {_currentXP}");
        }
        
        public void DeductXP(int amount)
        {
            if (amount <= 0 || _currentXP <= 0) return;
            
            _previousXP = _currentXP;
            _currentXP = Mathf.Max(0, _currentXP - amount);
            
            PublishXPChanged("XP Deduction");
            
            Debug.Log($"Player deducted {amount} XP. Total XP: {_currentXP}");
        }

        public void ApplySkillEffect(ISkillEffect effect)
        {
            if (effect == null) return;
            
            _activeEffects.Add(effect);
            
            // Apply the effect based on its type
            switch (effect.Type)
            {
                case SkillEffectType.Health:
                    ApplyHealthEffect(effect);
                    break;
                case SkillEffectType.Speed:
                    ApplySpeedEffect(effect);
                    break;
                case SkillEffectType.Damage:
                    ApplyDamageEffect(effect);
                    break;
                case SkillEffectType.AttackSpeed:
                    ApplyAttackSpeedEffect(effect);
                    break;
                case SkillEffectType.Range:
                    ApplyRangeEffect(effect);
                    break;
                case SkillEffectType.Defense:
                    ApplyDefenseEffect(effect);
                    break;
                case SkillEffectType.Utility:
                    ApplyUtilityEffect(effect);
                    break;
            }
            
            Debug.Log($"Applied skill effect: {effect.GetDescription()}. Total effects: {_activeEffects.Count}");
        }

        public void Update()
        {
            // Update timers
            _attackTimer += Time.deltaTime;
            
            // Update position tracking
            if (_config.Transform != null)
            {
                _lastKnownPosition = _config.Transform.position;
            }
        }

        public Vector3 GetMovementInput()
        {
            // GameManager will call this to get desired movement
            return new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;
        }

        public bool ShouldAttack(out float attackDamage, out float attackRange)
        {
            attackDamage = AttackDamage;
            attackRange = AttackRange;
            
            return _attackTimer >= (1f / AttackRate);
        }

        #region Private Methods

        private void ApplyHealthEffect(ISkillEffect effect)
        {
            float value = effect.GetNumericValue();
            _modifiers.HealthModifier += value;
            
            // Heal player to maintain health ratio or heal fully if this increases max health
            float healthRatio = MaxHealth > 0 ? _currentHealth / (MaxHealth - value) : 1f;
            _currentHealth = Mathf.Min(MaxHealth, _currentHealth + value); // Add the health directly
            
            PublishHealthChanged();
        }

        private void ApplySpeedEffect(ISkillEffect effect)
        {
            _modifiers.SpeedMultiplier *= effect.GetNumericValue();
        }

        private void ApplyDamageEffect(ISkillEffect effect)
        {
            _modifiers.DamageModifier += effect.GetNumericValue();
        }

        private void ApplyAttackSpeedEffect(ISkillEffect effect)
        {
            _modifiers.AttackSpeedMultiplier *= effect.GetNumericValue();
        }

        private void ApplyRangeEffect(ISkillEffect effect)
        {
            _modifiers.RangeModifier += effect.GetNumericValue();
        }

        private void ApplyDefenseEffect(ISkillEffect effect)
        {
            _modifiers.DamageReduction += effect.GetNumericValue();
        }

        private void ApplyUtilityEffect(ISkillEffect effect)
        {
            // Handle XP multiplier and other utility effects
            if (effect.GetDescription().Contains("XP"))
            {
                _modifiers.XPMultiplier *= effect.GetNumericValue();
            }
        }
        

        private void HandlePlayerDeath()
        {
            var gameOverEvent = new GameOverEvent
            {
                Victory = false,
                Reason = "Player health reached zero",
                FinalWave = 0, 
                TotalXP = _currentXP,
                SkillsUnlocked = _activeEffects.Count,
                TotalSurvivalTime = Time.time,
                Statistics = new Dictionary<string, int>
                {
                    ["TotalDamageTaken"] = Mathf.RoundToInt(_config.MaxHealth - _currentHealth),
                    ["SkillsUnlocked"] = _activeEffects.Count,
                    ["TotalXPEarned"] = _currentXP
                }
            };
            
            _eventBus.Publish(gameOverEvent);
            Debug.Log("Player has died!");
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            AddXP(evt.XPReward);
        }

        private void PublishHealthChanged()
        {
            _eventBus.Publish(new PlayerHealthChangedEvent
            {
                CurrentHealth = _currentHealth,
                MaxHealth = MaxHealth,
                PreviousHealth = _previousHealth,
            });
        }

        private void PublishXPChanged(string source)
        {
            _eventBus.Publish(new PlayerXPChangedEvent
            {
                CurrentXP = _currentXP,
                XPGained = _currentXP - _previousXP,
                PreviousXP = _previousXP,
                Source = source
            });
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Get comprehensive player statistics for debugging
        /// </summary>
        public Dictionary<string, object> GetPlayerStatistics()
        {
            return new Dictionary<string, object>
            {
                ["Health"] = $"{Health:F1}/{MaxHealth:F1}",
                ["XP"] = XP,
                ["Speed"] = $"{Speed:F1}",
                ["AttackDamage"] = $"{AttackDamage:F1}",
                ["AttackRange"] = $"{AttackRange:F1}",
                ["AttackRate"] = $"{AttackRate:F1}/sec",
                ["DamageReduction"] = $"{DamageReduction * 100:F1}%",
                ["XPMultiplier"] = $"{XPMultiplier:F2}x",
                ["ActiveEffects"] = ActiveEffectCount,
                ["IsAlive"] = IsAlive,
                ["IsCriticalHealth"] = IsCriticalHealth,
                ["Position"] = Position.ToString()
            };
        }

        /// <summary>
        /// Print detailed player stats to console
        /// </summary>
        public void PrintDetailedStats()
        {
            Debug.Log("--- DETAILED PLAYER STATS ---");
            var stats = GetPlayerStatistics();
            foreach (var stat in stats)
            {
                Debug.Log($"{stat.Key}: {stat.Value}");
            }
            Debug.Log("--- ACTIVE SKILL EFFECTS ---");
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                Debug.Log($"{i + 1}. {_activeEffects[i].GetDescription()}");
            }
        }

        /// <summary>
        /// Debug method to add XP for testing
        /// </summary>
        public void DebugAddXP(int amount)
        {
            AddXP(amount);
        }

        /// <summary>
        /// Debug method to take damage for testing
        /// </summary>
        public void DebugTakeDamage(float amount)
        {
            TakeDamage(amount);
        }

        /// <summary>
        /// Debug method to heal for testing
        /// </summary>
        public void DebugHeal(float amount)
        {
            Heal(amount);
        }

        #endregion
    }

    /// <summary>
    /// Internal class to track all player modifiers from skills
    /// Keeps modifier logic organized and easily debuggable
    /// </summary>
    internal class PlayerModifiers
    {
        public float HealthModifier { get; set; } = 0f;
        public float SpeedMultiplier { get; set; } = 1f;
        public float DamageModifier { get; set; } = 0f;
        public float RangeModifier { get; set; } = 0f;
        public float AttackSpeedMultiplier { get; set; } = 1f;
        public float DamageReduction { get; set; } = 0f;
        public float XPMultiplier { get; set; } = 1f;
        
        public void Reset()
        {
            HealthModifier = 0f;
            SpeedMultiplier = 1f;
            DamageModifier = 0f;
            RangeModifier = 0f;
            AttackSpeedMultiplier = 1f;
            DamageReduction = 0f;
            XPMultiplier = 1f;
        }
    }
}