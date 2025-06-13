using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// SkillSystem manages player skills, including unlocking, checking availability, and applying effects.
    /// </summary>
    public class SkillSystem : ISkillSystem
    {
        private readonly IEventBus _eventBus;
        private readonly IPlayerSystem _playerSystem;
        private readonly HashSet<string> _unlockedSkills = new HashSet<string>();
        private readonly List<SkillDefinition> _availableSkills;
        private readonly Dictionary<string, SkillDefinition> _skillLookup = new Dictionary<string, SkillDefinition>();
        private readonly Queue<ICommand> _commandHistory = new Queue<ICommand>();
        
        // Skill unlocking statistics
        private int _totalSkillsUnlocked = 0;
        private int _totalXPSpent = 0;
        private readonly Dictionary<SkillCategory, int> _skillsPerCategory = new Dictionary<SkillCategory, int>();
        
        public SkillSystem(IEventBus eventBus, IPlayerSystem playerSystem)
        {
            _eventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
            _playerSystem = playerSystem ?? throw new System.ArgumentNullException(nameof(playerSystem));
            
            // Initialize available skills from factory
            _availableSkills = SkillFactory.CreateAllSkills();
            
            // Create lookup dictionary for fast access
            foreach (var skill in _availableSkills)
            {
                _skillLookup[skill.Id] = skill;
                skill.IsUnlocked = false;
            }
            
            // Initialize category counters
            foreach (SkillCategory category in System.Enum.GetValues(typeof(SkillCategory)))
            {
                _skillsPerCategory[category] = 0;
            }
            
            Debug.Log($"SkillSystem initialized with {_availableSkills.Count} skills available");
        }

        public bool CanUnlockSkill(SkillDefinition skill)
        {
            if (skill == null)
            {
                Debug.LogWarning("CanUnlockSkill called with null skill");
                return false;
            }

            // Check if already unlocked
            if (IsSkillUnlocked(skill.Id))
            {
                return false;
            }

            // Check XP cost
            if (GetPlayerXP() < skill.Cost)
            {
                return false;
            }

            // Check prerequisites
            if (!ArePrerequisitesMet(skill))
            {
                return false;
            }

            return true;
        }

        public void UnlockSkill(SkillDefinition skill)
        {
            if (!CanUnlockSkill(skill))
            {
                Debug.LogWarning($"Cannot unlock skill: {skill?.Name ?? "null"}");
                return;
            }

            // Create and execute unlock command
            var command = new UnlockSkillCommand(this, _playerSystem, skill);
            if (command.CanExecute())
            {
                command.Execute();
                
                // Store command for potential undo
                _commandHistory.Enqueue(command);
                
                // Keep only last 10 commands
                if (_commandHistory.Count > 10)
                {
                    _commandHistory.Dequeue();
                }
            }
        }

        public bool IsSkillUnlocked(string skillId)
        {
            return !string.IsNullOrEmpty(skillId) && _unlockedSkills.Contains(skillId);
        }

        public int GetPlayerXP()
        {
            return _playerSystem?.XP ?? 0;
        }

        public void Update()
        {
            // Update skill availability indicators
            // This could be optimized to only update when XP or unlocked skills change
            UpdateSkillAvailability();
            
            // Process any pending skill effects
            ProcessPendingEffects();
        }

        public SkillDefinition GetSkillById(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            
            _skillLookup.TryGetValue(skillId, out var skill);
            return skill;
        }

        public List<SkillDefinition> GetAllSkills()
        {
            return new List<SkillDefinition>(_availableSkills);
        }

        public List<SkillDefinition> GetUnlockedSkills()
        {
            return _availableSkills.Where(s => s.IsUnlocked).ToList();
        }

        public List<SkillDefinition> GetAvailableSkills()
        {
            return _availableSkills.Where(CanUnlockSkill).ToList();
        }

        #region Internal Methods (Used by Commands)

        /// <summary>
        /// Internal method used by UnlockSkillCommand
        /// Should not be called directly - use UnlockSkill instead
        /// </summary>
        internal void InternalUnlockSkill(SkillDefinition skill)
        {
            if (skill == null) return;
            
            // Mark skill as unlocked
            _unlockedSkills.Add(skill.Id);
            skill.IsUnlocked = true;
            
            // Update statistics
            _totalSkillsUnlocked++;
            _totalXPSpent += skill.Cost;
            _skillsPerCategory[skill.Category]++;
            
            // Apply skill effect to player
            _playerSystem.ApplySkillEffect(skill.Effect);
            
            Debug.Log($"Skill unlocked: {skill.Name} (Total: {_totalSkillsUnlocked})");
            
            // Publish unlock event
            _eventBus.Publish(new SkillUnlockedEvent
            {
                Skill = skill,
                XPSpent = skill.Cost,
                RemainingXP = GetPlayerXP() - skill.Cost,
                TotalSkillsUnlocked = _totalSkillsUnlocked
            });
        }

        /// <summary>
        /// Internal method to remove a skill (for undo functionality)
        /// </summary>
        internal void InternalRemoveSkill(SkillDefinition skill)
        {
            if (skill == null || !IsSkillUnlocked(skill.Id)) return;
            
            _unlockedSkills.Remove(skill.Id);
            skill.IsUnlocked = false;
            
            // Update statistics
            _totalSkillsUnlocked--;
            _totalXPSpent -= skill.Cost;
            _skillsPerCategory[skill.Category]--;
            
            Debug.Log($"Skill removed: {skill.Name} (for undo)");
        }

        #endregion

        #region Private Methods

        private bool ArePrerequisitesMet(SkillDefinition skill)
        {
            if (skill?.Prerequisites == null) return true;
            
            foreach (var prerequisite in skill.Prerequisites)
            {
                if (!IsSkillUnlocked(prerequisite))
                {
                    return false;
                }
            }
            return true;
        }

        private void UpdateSkillAvailability()
        {
            // This could trigger UI updates or other system notifications
            // For now, it's mainly for internal state management
        }

        private void ProcessPendingEffects()
        {
            // Handle any time-based or conditional skill effects
            // For now, all effects are immediate, but this is where 
            // temporary effects would be processed
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Gets skills by category
        /// </summary>
        public List<SkillDefinition> GetSkillsByCategory(SkillCategory category)
        {
            return _availableSkills.Where(s => s.Category == category).ToList();
        }

        /// <summary>
        /// Gets skills by tier
        /// </summary>
        public List<SkillDefinition> GetSkillsByTier(SkillTier tier)
        {
            return _availableSkills.Where(s => s.Tier == tier).ToList();
        }

        /// <summary>
        /// Gets skills that are currently affordable
        /// </summary>
        public List<SkillDefinition> GetAffordableSkills()
        {
            int playerXP = GetPlayerXP();
            return _availableSkills.Where(s => !s.IsUnlocked && s.Cost <= playerXP).ToList();
        }

        /// <summary>
        /// Gets skills that are blocked by prerequisites
        /// </summary>
        public List<SkillDefinition> GetBlockedSkills()
        {
            return _availableSkills.Where(s => !s.IsUnlocked && !ArePrerequisitesMet(s)).ToList();
        }

        /// <summary>
        /// Gets the next recommended skill for a given category
        /// </summary>
        public SkillDefinition GetRecommendedSkill(SkillCategory category)
        {
            var availableInCategory = GetAvailableSkills()
                .Where(s => s.Category == category)
                .OrderBy(s => s.Cost)
                .ToList();
                
            return availableInCategory.FirstOrDefault();
        }

        /// <summary>
        /// Gets comprehensive skill tree statistics
        /// </summary>
        public SkillTreeStatistics GetSkillTreeStatistics()
        {
            var stats = new SkillTreeStatistics
            {
                TotalSkills = _availableSkills.Count,
                UnlockedSkills = _totalSkillsUnlocked,
                TotalXPSpent = _totalXPSpent,
                AvailableSkills = GetAvailableSkills().Count,
                AffordableSkills = GetAffordableSkills().Count,
                BlockedSkills = GetBlockedSkills().Count,
                CompletionPercentage = (float)_totalSkillsUnlocked / _availableSkills.Count * 100f
            };

            // Add per-category statistics
            foreach (var category in _skillsPerCategory)
            {
                var totalInCategory = _availableSkills.Count(s => s.Category == category.Key);
                var unlockedInCategory = category.Value;
                
                stats.CategoryProgress[category.Key] = new CategoryProgress
                {
                    Total = totalInCategory,
                    Unlocked = unlockedInCategory,
                    Percentage = totalInCategory > 0 ? (float)unlockedInCategory / totalInCategory * 100f : 0f
                };
            }

            return stats;
        }

        /// <summary>
        /// Undo the last skill unlock (for testing/debugging)
        /// </summary>
        public void UndoLastSkillUnlock()
        {
            if (_commandHistory.Count > 0)
            {
                // Get the most recent command without removing it from queue
                var commands = _commandHistory.ToArray();
                var lastCommand = commands.Last();
                
                lastCommand.Undo();
                Debug.Log("Undid last skill unlock");
            }
            else
            {
                Debug.Log("No skill unlocks to undo");
            }
        }

        /// <summary>
        /// Reset all skills (for testing/debugging)
        /// </summary>
        public void ResetAllSkills()
        {
            _unlockedSkills.Clear();
            _totalSkillsUnlocked = 0;
            _totalXPSpent = 0;
            
            foreach (var skill in _availableSkills)
            {
                skill.IsUnlocked = false;
            }
            
            foreach (var category in _skillsPerCategory.Keys.ToList())
            {
                _skillsPerCategory[category] = 0;
            }
            
            _commandHistory.Clear();
            
            Debug.Log("All skills reset");
        }

        /// <summary>
        /// Validates skill tree integrity
        /// </summary>
        public bool ValidateSkillTree()
        {
            foreach (var skill in _availableSkills)
            {
                foreach (var prerequisite in skill.Prerequisites)
                {
                    if (GetSkillById(prerequisite) == null)
                    {
                        Debug.LogError($"Skill '{skill.Id}' has invalid prerequisite '{prerequisite}'");
                        return false;
                    }
                }
            }
            
            Debug.Log("Skill tree validation passed");
            return true;
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Print detailed skill system information
        /// </summary>
        public void PrintSkillSystemInfo()
        {
            var stats = GetSkillTreeStatistics();
            
            Debug.Log("=== SKILL SYSTEM INFO ===");
            Debug.Log($"Total Skills: {stats.TotalSkills}");
            Debug.Log($"Unlocked: {stats.UnlockedSkills} ({stats.CompletionPercentage:F1}%)");
            Debug.Log($"Available: {stats.AvailableSkills}");
            Debug.Log($"Affordable: {stats.AffordableSkills}");
            Debug.Log($"Blocked: {stats.BlockedSkills}");
            Debug.Log($"Total XP Spent: {stats.TotalXPSpent}");
            
            Debug.Log("=== CATEGORY PROGRESS ===");
            foreach (var category in stats.CategoryProgress)
            {
                Debug.Log($"{category.Key}: {category.Value.Unlocked}/{category.Value.Total} ({category.Value.Percentage:F1}%)");
            }
            
            if (_totalSkillsUnlocked > 0)
            {
                Debug.Log("=== UNLOCKED SKILLS ===");
                var unlockedSkills = GetUnlockedSkills();
                foreach (var skill in unlockedSkills)
                {
                    Debug.Log($"- {skill.Name} ({skill.Category})");
                }
            }
        }

        #endregion
    }

    #region Command Pattern Implementation

    /// <summary>
    /// Pure C# Command Pattern - Encapsulates skill unlock as an object
    /// Allows for undo functionality and command queuing
    /// NO Unity dependencies
    /// </summary>
    public class UnlockSkillCommand : ICommand
    {
        private readonly SkillSystem _skillSystem;
        private readonly IPlayerSystem _playerSystem;
        private readonly SkillDefinition _skill;
        private bool _wasExecuted;
        private int _playerXPBefore;

        public UnlockSkillCommand(SkillSystem skillSystem, IPlayerSystem playerSystem, SkillDefinition skill)
        {
            _skillSystem = skillSystem ?? throw new System.ArgumentNullException(nameof(skillSystem));
            _playerSystem = playerSystem ?? throw new System.ArgumentNullException(nameof(playerSystem));
            _skill = skill ?? throw new System.ArgumentNullException(nameof(skill));
        }

        public bool CanExecute()
        {
            return _skillSystem.CanUnlockSkill(_skill);
        }

        public void Execute()
        {
            if (!CanExecute())
            {
                Debug.LogWarning($"Cannot execute unlock command for skill: {_skill.Name}");
                return;
            }

            _playerXPBefore = _playerSystem.XP;
            
            // Note: In a full implementation, this would deduct XP from the player
            // For the prototype, we're not actually deducting XP to make testing easier
            
            // Mark skill as unlocked in the system
            _skillSystem.InternalUnlockSkill(_skill);
            
            _wasExecuted = true;
            
            Debug.Log($"Executed unlock command for skill: {_skill.Name}");
        }

        public void Undo()
        {
            if (!_wasExecuted)
            {
                Debug.LogWarning($"Cannot undo - command was not executed: {_skill.Name}");
                return;
            }

            // Remove the skill from unlocked skills
            _skillSystem.InternalRemoveSkill(_skill);
            
            // In a full implementation, you would:
            // 1. Restore the XP to the player
            // 2. Remove the skill effect from the player
            // 3. Update the UI
            
            Debug.Log($"Undoing skill unlock: {_skill.Name}");
            
            _wasExecuted = false;
        }

        public string GetDescription()
        {
            return $"Unlock {_skill.Name} for {_skill.Cost} XP";
        }
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// Comprehensive skill tree statistics
    /// </summary>
    public class SkillTreeStatistics
    {
        public int TotalSkills { get; set; }
        public int UnlockedSkills { get; set; }
        public int TotalXPSpent { get; set; }
        public int AvailableSkills { get; set; }
        public int AffordableSkills { get; set; }
        public int BlockedSkills { get; set; }
        public float CompletionPercentage { get; set; }
        public Dictionary<SkillCategory, CategoryProgress> CategoryProgress { get; set; } = new Dictionary<SkillCategory, CategoryProgress>();
    }

    /// <summary>
    /// Progress tracking for each skill category
    /// </summary>
    public class CategoryProgress
    {
        public int Total { get; set; }
        public int Unlocked { get; set; }
        public float Percentage { get; set; }
    }

    #endregion
}