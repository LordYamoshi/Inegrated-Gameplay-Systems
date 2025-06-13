using UnityEngine;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Interface for managing the user interface system
    /// </summary>
    public interface IUISystem
    {
        void Initialize(IEventBus eventBus);
        void UpdateHealthDisplay(float current, float max);
        void UpdateXPDisplay(int xp);
        void UpdateWaveDisplay(int wave);
        void ShowGameOver(bool victory, int finalWave, int totalXP);
        void ShowSkillUnlocked(SkillDefinition skill);
    }
}