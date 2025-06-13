using UnityEngine;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Interface for skill effects that can be applied to players
    /// </summary>
    public interface ISkillEffect
    {
        void Apply(IPlayerSystem player);
        string GetDescription();
        SkillEffectType Type { get; }
        float GetNumericValue();
    }
}