using UnityEngine;

namespace SkillTreeSurvivor
{
    #region Base Effect Classes

    /// <summary>
    /// Base class for all skill effects
    /// </summary>
    public abstract class BaseSkillEffect : ISkillEffect
    {
        protected readonly float _value;
        protected readonly string _description;
        protected readonly SkillEffectType _type;

        protected BaseSkillEffect(float value, string description, SkillEffectType type)
        {
            _value = value;
            _description = description;
            _type = type;
        }

        public abstract void Apply(IPlayerSystem player);
        public virtual string GetDescription() => _description;
        public virtual SkillEffectType Type => _type;
        public virtual float GetNumericValue() => _value;
    }

    #endregion

    #region Health Effects

    /// <summary>
    /// Increases player's maximum health
    /// </summary>
    public class HealthBoostEffect : BaseSkillEffect
    {
        public HealthBoostEffect(float healthIncrease) 
            : base(healthIncrease, $"+{healthIncrease} Max Health", SkillEffectType.Health)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            // The PlayerSystem will handle the actual stat modification
            // This method is called by the PlayerSystem to trigger the effect
            Debug.Log($"Applied Health Boost: +{_value} HP");
        }
    }

    /// <summary>
    /// Provides instant health restoration
    /// </summary>
    public class InstantHealEffect : BaseSkillEffect
    {
        public InstantHealEffect(float healAmount) 
            : base(healAmount, $"Restore {healAmount} Health", SkillEffectType.Health)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            player.Heal(_value);
            Debug.Log($"Applied Instant Heal: +{_value} HP restored");
        }
    }

    /// <summary>
    /// Provides health regeneration over time
    /// </summary>
    public class HealthRegenerationEffect : BaseSkillEffect
    {
        public HealthRegenerationEffect(float regenPerSecond) 
            : base(regenPerSecond, $"+{regenPerSecond} HP/sec", SkillEffectType.Health)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            // This would be handled by a time-based system in a full implementation
            Debug.Log($"Applied Health Regeneration: +{_value} HP/sec");
        }
    }

    #endregion

    #region Movement Effects

    /// <summary>
    /// Increases player movement speed by a multiplier
    /// </summary>
    public class SpeedBoostEffect : BaseSkillEffect
    {
        public SpeedBoostEffect(float speedMultiplier) 
            : base(speedMultiplier, $"+{(speedMultiplier - 1) * 100:F0}% Movement Speed", SkillEffectType.Speed)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Speed Boost: +{(_value - 1) * 100:F0}% Speed");
        }
    }

    /// <summary>
    /// Provides a flat speed increase
    /// </summary>
    public class FlatSpeedEffect : BaseSkillEffect
    {
        public FlatSpeedEffect(float speedIncrease) 
            : base(speedIncrease, $"+{speedIncrease} Movement Speed", SkillEffectType.Speed)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Flat Speed Increase: +{_value} Speed");
        }
    }

    #endregion

    #region Combat Effects

    /// <summary>
    /// Increases player damage output
    /// </summary>
    public class DamageBoostEffect : BaseSkillEffect
    {
        public DamageBoostEffect(float damageIncrease) 
            : base(damageIncrease, $"+{damageIncrease} Attack Damage", SkillEffectType.Damage)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Damage Boost: +{_value} Attack Damage");
        }
    }

    /// <summary>
    /// Increases attack speed/frequency
    /// </summary>
    public class AttackSpeedEffect : BaseSkillEffect
    {
        public AttackSpeedEffect(float attackSpeedMultiplier) 
            : base(attackSpeedMultiplier, $"+{(attackSpeedMultiplier - 1) * 100:F0}% Attack Speed", SkillEffectType.AttackSpeed)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Attack Speed: +{(_value - 1) * 100:F0}% Attack Speed");
        }
    }

    /// <summary>
    /// Increases attack range
    /// </summary>
    public class RangeBoostEffect : BaseSkillEffect
    {
        public RangeBoostEffect(float rangeIncrease) 
            : base(rangeIncrease, $"+{rangeIncrease} Attack Range", SkillEffectType.Range)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Range Boost: +{_value} Attack Range");
        }
    }

    /// <summary>
    /// Increases critical hit chance
    /// </summary>
    public class CriticalHitEffect : BaseSkillEffect
    {
        public CriticalHitEffect(float critChance) 
            : base(critChance, $"+{critChance * 100:F0}% Critical Hit Chance", SkillEffectType.Damage)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Critical Hit: +{_value * 100:F0}% Crit Chance");
        }
    }

    #endregion

    #region Defense Effects

    /// <summary>
    /// Reduces incoming damage by a percentage
    /// </summary>
    public class ArmorEffect : BaseSkillEffect
    {
        public ArmorEffect(float damageReduction) 
            : base(damageReduction, $"-{damageReduction * 100:F0}% Damage Taken", SkillEffectType.Defense)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Armor: -{_value * 100:F0}% Damage Taken");
        }
    }

    /// <summary>
    /// Provides flat damage reduction
    /// </summary>
    public class FlatArmorEffect : BaseSkillEffect
    {
        public FlatArmorEffect(float damageReduction) 
            : base(damageReduction, $"-{damageReduction} Damage Taken", SkillEffectType.Defense)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Flat Armor: -{_value} Damage Reduction");
        }
    }

    /// <summary>
    /// Provides damage immunity for a short duration
    /// </summary>
    public class ImmunityEffect : BaseSkillEffect
    {
        public ImmunityEffect(float duration) 
            : base(duration, $"{duration}s Damage Immunity", SkillEffectType.Defense)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Immunity: {_value}s invincibility");
        }
    }

    #endregion

    #region Utility Effects

    /// <summary>
    /// Increases XP gain from enemies
    /// </summary>
    public class XPBoostEffect : BaseSkillEffect
    {
        public XPBoostEffect(float xpMultiplier) 
            : base(xpMultiplier, $"+{(xpMultiplier - 1) * 100:F0}% XP Gain", SkillEffectType.Utility)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied XP Boost: +{(_value - 1) * 100:F0}% XP Gain");
        }
    }

    /// <summary>
    /// Provides a flat XP bonus
    /// </summary>
    public class FlatXPEffect : BaseSkillEffect
    {
        public FlatXPEffect(float xpBonus) 
            : base(xpBonus, $"+{xpBonus} XP per kill", SkillEffectType.Utility)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Flat XP Bonus: +{_value} XP per kill");
        }
    }

    /// <summary>
    /// Increases resource collection efficiency
    /// </summary>
    public class ResourceBoostEffect : BaseSkillEffect
    {
        public ResourceBoostEffect(float resourceMultiplier) 
            : base(resourceMultiplier, $"+{(resourceMultiplier - 1) * 100:F0}% Resource Collection", SkillEffectType.Utility)
        {
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Resource Boost: +{(_value - 1) * 100:F0}% Resources");
        }
    }

    #endregion

    #region Composite Effects

    /// <summary>
    /// Composite effect that applies multiple effects simultaneously
    /// Useful for powerful skills that affect multiple stats
    /// </summary>
    public class CompositeSkillEffect : ISkillEffect
    {
        private readonly ISkillEffect[] _effects;
        private readonly string _description;
        private readonly SkillEffectType _primaryType;
        
        public SkillEffectType Type => _primaryType;

        public CompositeSkillEffect(string description, SkillEffectType primaryType, params ISkillEffect[] effects)
        {
            _effects = effects ?? throw new System.ArgumentNullException(nameof(effects));
            _description = description;
            _primaryType = primaryType;
        }
        
        public void Apply(IPlayerSystem player)
        {
            foreach (var effect in _effects)
            {
                effect.Apply(player);
            }
            Debug.Log($"Applied Composite Effect: {_description}");
        }
        
        public string GetDescription() => _description;

        public float GetNumericValue()
        {
            // Return the sum of all numeric values for composite effects
            float total = 0f;
            foreach (var effect in _effects)
            {
                total += effect.GetNumericValue();
            }
            return total;
        }

        public ISkillEffect[] GetSubEffects() => _effects;
    }

    /// <summary>
    /// Conditional effect that only applies under certain circumstances
    /// </summary>
    public class ConditionalSkillEffect : BaseSkillEffect
    {
        private readonly System.Func<IPlayerSystem, bool> _condition;
        private readonly ISkillEffect _conditionalEffect;

        public ConditionalSkillEffect(System.Func<IPlayerSystem, bool> condition, ISkillEffect effect, string description) 
            : base(0f, description, effect.Type)
        {
            _condition = condition;
            _conditionalEffect = effect;
        }
        
        public override void Apply(IPlayerSystem player)
        {
            if (_condition(player))
            {
                _conditionalEffect.Apply(player);
                Debug.Log($"Applied Conditional Effect: {_description}");
            }
            else
            {
                Debug.Log($"Conditional Effect not triggered: {_description}");
            }
        }

        public override float GetNumericValue() => _conditionalEffect.GetNumericValue();
    }

    /// <summary>
    /// Temporary effect that lasts for a specific duration
    /// </summary>
    public class TemporarySkillEffect : BaseSkillEffect
    {
        private readonly float _duration;
        private readonly ISkillEffect _baseEffect;
        private float _remainingTime;
        private bool _isActive;

        public bool IsActive => _isActive && _remainingTime > 0;
        public float RemainingTime => _remainingTime;

        public TemporarySkillEffect(ISkillEffect baseEffect, float duration) 
            : base(0f, $"{baseEffect.GetDescription()} (for {duration}s)", baseEffect.Type)
        {
            _baseEffect = baseEffect;
            _duration = duration;
            _remainingTime = duration;
        }
        
        public override void Apply(IPlayerSystem player)
        {
            _baseEffect.Apply(player);
            _isActive = true;
            _remainingTime = _duration;
            Debug.Log($"Applied Temporary Effect: {_description}");
        }

        public void Update(float deltaTime)
        {
            if (_isActive)
            {
                _remainingTime -= deltaTime;
                if (_remainingTime <= 0)
                {
                    _isActive = false;
                    Debug.Log($"Temporary Effect Expired: {_description}");
                }
            }
        }

        public override float GetNumericValue() => _baseEffect.GetNumericValue();
    }

    #endregion

    #region Specialized Effects

    /// <summary>
    /// Synergy effect that becomes more powerful when other specific skills are unlocked
    /// </summary>
    public class SynergyEffect : BaseSkillEffect
    {
        private readonly string[] _requiredSkills;
        private readonly float _baseValue;
        private readonly float _synergyMultiplier;

        public SynergyEffect(float baseValue, float synergyMultiplier, string description, SkillEffectType type, params string[] requiredSkills) 
            : base(baseValue, description, type)
        {
            _baseValue = baseValue;
            _synergyMultiplier = synergyMultiplier;
            _requiredSkills = requiredSkills;
        }
        
        public override void Apply(IPlayerSystem player)
        {
            Debug.Log($"Applied Synergy Effect: {_description}");
        }

        public float GetSynergyValue(int unlockedSynergySkills)
        {
            return _baseValue * Mathf.Pow(_synergyMultiplier, unlockedSynergySkills);
        }

        public override float GetNumericValue() => _baseValue;
    }

    /// <summary>
    /// Scaling effect that becomes more powerful based on player level/XP
    /// </summary>
    public class ScalingEffect : BaseSkillEffect
    {
        private readonly float _baseValue;
        private readonly float _scalingFactor;
        private readonly int _scalingBase;

        public ScalingEffect(float baseValue, float scalingFactor, int scalingBase, string description, SkillEffectType type) 
            : base(baseValue, description, type)
        {
            _baseValue = baseValue;
            _scalingFactor = scalingFactor;
            _scalingBase = scalingBase;
        }
        
        public override void Apply(IPlayerSystem player)
        {
            float scaledValue = GetScaledValue(player.XP);
            Debug.Log($"Applied Scaling Effect: {_description} (scaled to {scaledValue:F1})");
        }

        public float GetScaledValue(int playerXP)
        {
            int scalingLevels = playerXP / _scalingBase;
            return _baseValue + (scalingLevels * _scalingFactor);
        }

        public override float GetNumericValue() => _baseValue;
    }

    #endregion
}