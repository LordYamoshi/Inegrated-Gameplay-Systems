using System.Collections.Generic;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Data structure representing a skill in the skill tree
    /// Contains all information needed to display and unlock skills
    /// </summary>
    [System.Serializable]
    public class SkillDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public ISkillEffect Effect { get; set; }
        public List<string> Prerequisites { get; set; } = new List<string>();
        public SkillTier Tier { get; set; }
        public SkillCategory Category { get; set; }
        public string IconPath { get; set; }
        public bool IsUnlocked { get; set; }
        
        /// <summary>
        /// Position in the skill tree UI (for layout purposes)
        /// </summary>
        public SkillPosition Position { get; set; }

        public SkillDefinition()
        {
            Prerequisites = new List<string>();
            Position = new SkillPosition();
        }

        public SkillDefinition(string id, string name, string description, int cost, ISkillEffect effect)
        {
            Id = id;
            Name = name;
            Description = description;
            Cost = cost;
            Effect = effect;
            Prerequisites = new List<string>();
            Position = new SkillPosition();
        }

        /// <summary>
        /// Checks if all prerequisites are met
        /// </summary>
        public bool ArePrerequisitesMet(ISkillSystem skillSystem)
        {
            foreach (var prerequisite in Prerequisites)
            {
                if (!skillSystem.IsSkillUnlocked(prerequisite))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the full description including effect details
        /// </summary>
        public string GetFullDescription()
        {
            var effectDesc = Effect?.GetDescription() ?? "No effect";
            return $"{Description}\n\nEffect: {effectDesc}\nCost: {Cost} XP";
        }
    }

    /// <summary>
    /// Skill tiers for organization and prerequisites
    /// </summary>
    public enum SkillTier
    {
        Basic = 1,
        Intermediate = 2,
        Advanced = 3,
        Master = 4,
        Legendary = 5
    }

    /// <summary>
    /// Skill categories for UI organization
    /// </summary>
    public enum SkillCategory
    {
        Combat,
        Defense,
        Utility,
        Movement,
        Special
    }

    /// <summary>
    /// Position data for skill tree layout
    /// </summary>
    [System.Serializable]
    public class SkillPosition
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public SkillPosition() { }

        public SkillPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public SkillPosition(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}