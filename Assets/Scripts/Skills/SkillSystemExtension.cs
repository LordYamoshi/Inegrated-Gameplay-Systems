using System.Collections.Generic;
using System.Linq;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Extension methods for ISkillSystem to provide convenient access to skill data
    /// </summary>
    public static class SkillSystemExtensions
    {
        /// <summary>
        /// Get skill definition by ID 
        /// Provides safe access to skill data for UI and other systems
        /// </summary>
        public static SkillDefinition GetSkillById(this ISkillSystem skillSystem, string skillId)
        {
            // Use the interface method directly since it's now part of ISkillSystem
            return skillSystem.GetSkillById(skillId);
        }
        
        /// <summary>
        /// Get all available skills
        /// </summary>
        public static List<SkillDefinition> GetAllSkills(this ISkillSystem skillSystem)
        {
            // Use the interface method directly
            return skillSystem.GetAllSkills();
        }
        
        /// <summary>
        /// Get unlocked skills 
        /// </summary>
        public static List<SkillDefinition> GetUnlockedSkills(this ISkillSystem skillSystem)
        {
            // Use the interface method directly
            return skillSystem.GetUnlockedSkills();
        }
        
        /// <summary>
        /// Get skills that can be unlocked right now
        /// </summary>
        public static List<SkillDefinition> GetAvailableSkills(this ISkillSystem skillSystem)
        {
            // Use the interface method directly
            return skillSystem.GetAvailableSkills();
        }
        
        /// <summary>
        /// Get skills by category
        /// </summary>
        public static List<SkillDefinition> GetSkillsByCategory(this ISkillSystem skillSystem, SkillCategory category)
        {
            return skillSystem.GetAllSkills().Where(s => s.Category == category).ToList();
        }

        /// <summary>
        /// Get skills by tier
        /// </summary>
        public static List<SkillDefinition> GetSkillsByTier(this ISkillSystem skillSystem, SkillTier tier)
        {
            return skillSystem.GetAllSkills().Where(s => s.Tier == tier).ToList();
        }

        /// <summary>
        /// Get skills that are affordable with current XP
        /// </summary>
        public static List<SkillDefinition> GetAffordableSkills(this ISkillSystem skillSystem)
        {
            int playerXP = skillSystem.GetPlayerXP();
            return skillSystem.GetAllSkills()
                .Where(s => !s.IsUnlocked && s.Cost <= playerXP)
                .ToList();
        }

        /// <summary>
        /// Get skills that are blocked by prerequisites
        /// </summary>
        public static List<SkillDefinition> GetBlockedSkills(this ISkillSystem skillSystem)
        {
            return skillSystem.GetAllSkills()
                .Where(s => !s.IsUnlocked && !s.ArePrerequisitesMet(skillSystem))
                .ToList();
        }

        /// <summary>
        /// Get skills that player has enough XP for but prerequisites aren't met
        /// </summary>
        public static List<SkillDefinition> GetSkillsAwaitingPrerequisites(this ISkillSystem skillSystem)
        {
            int playerXP = skillSystem.GetPlayerXP();
            return skillSystem.GetAllSkills()
                .Where(s => !s.IsUnlocked && 
                           s.Cost <= playerXP && 
                           !s.ArePrerequisitesMet(skillSystem))
                .ToList();
        }

        /// <summary>
        /// Get the next recommended skill for a given category based on cost and prerequisites
        /// </summary>
        public static SkillDefinition GetNextRecommendedSkill(this ISkillSystem skillSystem, SkillCategory category)
        {
            return skillSystem.GetAvailableSkills()
                .Where(s => s.Category == category)
                .OrderBy(s => s.Cost)
                .ThenBy(s => s.Tier)
                .FirstOrDefault();
        }

        /// <summary>
        /// Get skills that directly unlock when a specific skill is unlocked
        /// </summary>
        public static List<SkillDefinition> GetSkillsUnlockedBy(this ISkillSystem skillSystem, string skillId)
        {
            return skillSystem.GetAllSkills()
                .Where(s => s.Prerequisites.Contains(skillId))
                .ToList();
        }

        /// <summary>
        /// Get the skill unlock path to reach a target skill
        /// Returns the chain of skills needed to unlock the target
        /// </summary>
        public static List<SkillDefinition> GetUnlockPath(this ISkillSystem skillSystem, string targetSkillId)
        {
            var path = new List<SkillDefinition>();
            var visited = new HashSet<string>();
            
            BuildUnlockPath(skillSystem, targetSkillId, path, visited);
            
            return path.Distinct().ToList();
        }

        private static void BuildUnlockPath(ISkillSystem skillSystem, string skillId, 
            List<SkillDefinition> path, HashSet<string> visited)
        {
            if (visited.Contains(skillId)) return;
            visited.Add(skillId);
            
            var skill = skillSystem.GetSkillById(skillId);
            if (skill == null || skill.IsUnlocked) return;
            
            // First, add all prerequisites
            foreach (var prerequisite in skill.Prerequisites)
            {
                BuildUnlockPath(skillSystem, prerequisite, path, visited);
            }
            
            // Then add this skill
            path.Add(skill);
        }

        /// <summary>
        /// Calculate total XP cost to unlock a skill including all prerequisites
        /// </summary>
        public static int GetTotalUnlockCost(this ISkillSystem skillSystem, string skillId)
        {
            var unlockPath = skillSystem.GetUnlockPath(skillId);
            return unlockPath.Sum(s => s.Cost);
        }

        /// <summary>
        /// Check if player can eventually unlock a skill (has enough XP for full path)
        /// </summary>
        public static bool CanEventuallyUnlock(this ISkillSystem skillSystem, string skillId)
        {
            int playerXP = skillSystem.GetPlayerXP();
            int totalCost = skillSystem.GetTotalUnlockCost(skillId);
            return playerXP >= totalCost;
        }

        /// <summary>
        /// Get skills grouped by their unlock status for UI organization
        /// </summary>
        public static Dictionary<string, List<SkillDefinition>> GetSkillsByStatus(this ISkillSystem skillSystem)
        {
            var allSkills = skillSystem.GetAllSkills();
            
            return new Dictionary<string, List<SkillDefinition>>
            {
                ["Unlocked"] = allSkills.Where(s => s.IsUnlocked).ToList(),
                ["Available"] = skillSystem.GetAvailableSkills(),
                ["Affordable"] = skillSystem.GetAffordableSkills(),
                ["Blocked"] = skillSystem.GetBlockedSkills(),
                ["AwaitingPrerequisites"] = skillSystem.GetSkillsAwaitingPrerequisites()
            };
        }

        /// <summary>
        /// Get skill tree completion statistics
        /// </summary>
        public static SkillTreeProgress GetProgress(this ISkillSystem skillSystem)
        {
            var allSkills = skillSystem.GetAllSkills();
            var unlockedSkills = skillSystem.GetUnlockedSkills();
            
            var progress = new SkillTreeProgress
            {
                TotalSkills = allSkills.Count,
                UnlockedSkills = unlockedSkills.Count,
                CompletionPercentage = allSkills.Count > 0 ? 
                    (float)unlockedSkills.Count / allSkills.Count * 100f : 0f,
                AvailableSkills = skillSystem.GetAvailableSkills().Count,
                PlayerXP = skillSystem.GetPlayerXP()
            };

            // Calculate per-category progress
            foreach (SkillCategory category in System.Enum.GetValues(typeof(SkillCategory)))
            {
                var categorySkills = allSkills.Where(s => s.Category == category).ToList();
                var categoryUnlocked = unlockedSkills.Where(s => s.Category == category).ToList();
                
                progress.CategoryProgress[category] = new CategorySkillProgress
                {
                    Total = categorySkills.Count,
                    Unlocked = categoryUnlocked.Count,
                    Percentage = categorySkills.Count > 0 ? 
                        (float)categoryUnlocked.Count / categorySkills.Count * 100f : 0f,
                    NextRecommended = skillSystem.GetNextRecommendedSkill(category)
                };
            }

            // Calculate per-tier progress
            foreach (SkillTier tier in System.Enum.GetValues(typeof(SkillTier)))
            {
                var tierSkills = allSkills.Where(s => s.Tier == tier).ToList();
                var tierUnlocked = unlockedSkills.Where(s => s.Tier == tier).ToList();
                
                progress.TierProgress[tier] = new TierSkillProgress
                {
                    Total = tierSkills.Count,
                    Unlocked = tierUnlocked.Count,
                    Percentage = tierSkills.Count > 0 ? 
                        (float)tierUnlocked.Count / tierSkills.Count * 100f : 0f
                };
            }

            return progress;
        }

        /// <summary>
        /// Find optimal skill unlock order for maximum efficiency
        /// </summary>
        public static List<SkillDefinition> GetOptimalUnlockOrder(this ISkillSystem skillSystem, 
            SkillCategory priorityCategory = SkillCategory.Combat)
        {
            var availableSkills = skillSystem.GetAvailableSkills();
            
            // Sort by priority: category match, then cost, then tier
            return availableSkills
                .OrderByDescending(s => s.Category == priorityCategory ? 1 : 0)
                .ThenBy(s => s.Cost)
                .ThenBy(s => (int)s.Tier)
                .ToList();
        }

        /// <summary>
        /// Validate skill tree integrity and report any issues
        /// </summary>
        public static List<string> ValidateSkillTree(this ISkillSystem skillSystem)
        {
            var issues = new List<string>();
            var allSkills = skillSystem.GetAllSkills();
            var skillIds = allSkills.Select(s => s.Id).ToHashSet();

            foreach (var skill in allSkills)
            {
                // Check for invalid prerequisites
                foreach (var prerequisite in skill.Prerequisites)
                {
                    if (!skillIds.Contains(prerequisite))
                    {
                        issues.Add($"Skill '{skill.Id}' has invalid prerequisite '{prerequisite}'");
                    }
                }

                // Check for self-references
                if (skill.Prerequisites.Contains(skill.Id))
                {
                    issues.Add($"Skill '{skill.Id}' cannot be its own prerequisite");
                }

                // Check for basic data integrity
                if (string.IsNullOrEmpty(skill.Name))
                {
                    issues.Add($"Skill '{skill.Id}' has no name");
                }

                if (skill.Cost < 0)
                {
                    issues.Add($"Skill '{skill.Id}' has negative cost");
                }

                if (skill.Effect == null)
                {
                    issues.Add($"Skill '{skill.Id}' has no effect");
                }
            }

            return issues;
        }
    }

    #region Supporting Data Structures

    /// <summary>
    /// Overall skill tree progress information
    /// </summary>
    public class SkillTreeProgress
    {
        public int TotalSkills { get; set; }
        public int UnlockedSkills { get; set; }
        public float CompletionPercentage { get; set; }
        public int AvailableSkills { get; set; }
        public int PlayerXP { get; set; }
        public Dictionary<SkillCategory, CategorySkillProgress> CategoryProgress { get; set; } = 
            new Dictionary<SkillCategory, CategorySkillProgress>();
        public Dictionary<SkillTier, TierSkillProgress> TierProgress { get; set; } = 
            new Dictionary<SkillTier, TierSkillProgress>();
    }

    /// <summary>
    /// Progress information for a specific skill category
    /// </summary>
    public class CategorySkillProgress
    {
        public int Total { get; set; }
        public int Unlocked { get; set; }
        public float Percentage { get; set; }
        public SkillDefinition NextRecommended { get; set; }
    }

    /// <summary>
    /// Progress information for a specific skill tier
    /// </summary>
    public class TierSkillProgress
    {
        public int Total { get; set; }
        public int Unlocked { get; set; }
        public float Percentage { get; set; }
    }

    #endregion
}