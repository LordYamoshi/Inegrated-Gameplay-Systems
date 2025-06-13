using UnityEngine;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Interface for managing player skills, including unlocking and checking skill availability
    /// </summary>
    public interface ISkillSystem
    {
        bool CanUnlockSkill(SkillDefinition skill);
        void UnlockSkill(SkillDefinition skill);
        bool IsSkillUnlocked(string skillId);
        int GetPlayerXP();
        void Update();
        SkillDefinition GetSkillById(string skillId);
        System.Collections.Generic.List<SkillDefinition> GetAllSkills();
        System.Collections.Generic.List<SkillDefinition> GetUnlockedSkills();
        System.Collections.Generic.List<SkillDefinition> GetAvailableSkills();
    }
}