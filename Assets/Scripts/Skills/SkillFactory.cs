using System.Collections.Generic;
using System.Linq;

namespace SkillTreeSurvivor
{
    /// <summary>
    // Factory class to create and manage skills in the skill tree
    /// </summary>
    public static class SkillFactory
    {
        // Skill balancing constants
        private const int BASE_SKILL_COST = 10;
        private const float TIER_COST_MULTIPLIER = 2.5f;
        private const float HEALTH_BASE_VALUE = 25f;
        private const float DAMAGE_BASE_VALUE = 10f;
        private const float SPEED_BASE_VALUE = 1.2f;

        /// <summary>
        /// Creates all available skills for the game
        /// Organized by tiers for progressive unlocking
        /// </summary>
        public static List<SkillDefinition> CreateAllSkills()
        {
            var skills = new List<SkillDefinition>();

            // Create skills by tier for balanced progression
            skills.AddRange(CreateTier1Skills());
            skills.AddRange(CreateTier2Skills());
            skills.AddRange(CreateTier3Skills());
            skills.AddRange(CreateTier4Skills());

            // Validate skill tree integrity
            ValidateSkillTree(skills);

            return skills;
        }

        #region Tier 1 - Foundation Skills (No prerequisites)

        private static List<SkillDefinition> CreateTier1Skills()
        {
            return new List<SkillDefinition>
            {
                CreateSkill(
                    id: "vitality_1",
                    name: "Vitality",
                    description: "Increase your maximum health to survive longer",
                    tier: SkillTier.Basic,
                    category: SkillCategory.Defense,
                    cost: BASE_SKILL_COST,
                    effect: new HealthBoostEffect(HEALTH_BASE_VALUE),
                    position: new SkillPosition(0, 0)
                ),
                
                CreateSkill(
                    id: "swift_feet_1",
                    name: "Swift Feet",
                    description: "Move faster across the battlefield",
                    tier: SkillTier.Basic,
                    category: SkillCategory.Movement,
                    cost: BASE_SKILL_COST + 2,
                    effect: new SpeedBoostEffect(SPEED_BASE_VALUE),
                    position: new SkillPosition(0, 1)
                ),
                
                CreateSkill(
                    id: "sharp_strike_1",
                    name: "Sharp Strike",
                    description: "Deal more damage to enemies",
                    tier: SkillTier.Basic,
                    category: SkillCategory.Combat,
                    cost: BASE_SKILL_COST,
                    effect: new DamageBoostEffect(DAMAGE_BASE_VALUE),
                    position: new SkillPosition(0, 2)
                ),
                
                CreateSkill(
                    id: "quick_learner_1",
                    name: "Quick Learner",
                    description: "Gain experience faster from defeated enemies",
                    tier: SkillTier.Basic,
                    category: SkillCategory.Utility,
                    cost: BASE_SKILL_COST + 5,
                    effect: new XPBoostEffect(1.25f),
                    position: new SkillPosition(0, 3)
                ),

                CreateSkill(
                    id: "reach_1",
                    name: "Extended Reach",
                    description: "Attack enemies from a greater distance",
                    tier: SkillTier.Basic,
                    category: SkillCategory.Combat,
                    cost: BASE_SKILL_COST,
                    effect: new RangeBoostEffect(1f),
                    position: new SkillPosition(0, 4)
                )
            };
        }

        #endregion

        #region Tier 2 - Intermediate Skills (Require Tier 1)

        private static List<SkillDefinition> CreateTier2Skills()
        {
            int tier2Cost = (int)(BASE_SKILL_COST * TIER_COST_MULTIPLIER);
            
            return new List<SkillDefinition>
            {
                CreateSkill(
                    id: "iron_constitution",
                    name: "Iron Constitution",
                    description: "Further increase your health reserves",
                    tier: SkillTier.Intermediate,
                    category: SkillCategory.Defense,
                    cost: tier2Cost,
                    effect: new HealthBoostEffect(HEALTH_BASE_VALUE * 2f),
                    prerequisites: new[] { "vitality_1" },
                    position: new SkillPosition(1, 0)
                ),
                
                CreateSkill(
                    id: "tough_skin",
                    name: "Tough Skin",
                    description: "Reduce incoming damage",
                    tier: SkillTier.Intermediate,
                    category: SkillCategory.Defense,
                    cost: tier2Cost + 5,
                    effect: new ArmorEffect(0.15f), // 15% damage reduction
                    prerequisites: new[] { "vitality_1" },
                    position: new SkillPosition(1, 1)
                ),
                
                CreateSkill(
                    id: "combat_reflexes",
                    name: "Combat Reflexes",
                    description: "Attack more frequently",
                    tier: SkillTier.Intermediate,
                    category: SkillCategory.Combat,
                    cost: tier2Cost,
                    effect: new AttackSpeedEffect(1.4f), // 40% faster attacks
                    prerequisites: new[] { "sharp_strike_1" },
                    position: new SkillPosition(1, 2)
                ),
                
                CreateSkill(
                    id: "agility_master",
                    name: "Agility Master",
                    description: "Significant speed improvement",
                    tier: SkillTier.Intermediate,
                    category: SkillCategory.Movement,
                    cost: tier2Cost,
                    effect: new SpeedBoostEffect(1.4f), // Additional 40% speed
                    prerequisites: new[] { "swift_feet_1" },
                    position: new SkillPosition(1, 3)
                ),

                CreateSkill(
                    id: "scholar_focus",
                    name: "Scholar's Focus",
                    description: "Enhanced learning and quick recovery",
                    tier: SkillTier.Intermediate,
                    category: SkillCategory.Utility,
                    cost: tier2Cost,
                    effect: new CompositeSkillEffect(
                        "Scholar Mode: +50% XP, Restore 30 Health",
                        SkillEffectType.Utility,
                        new XPBoostEffect(1.5f),
                        new InstantHealEffect(30f)
                    ),
                    prerequisites: new[] { "quick_learner_1" },
                    position: new SkillPosition(1, 4)
                )
            };
        }

        #endregion

        #region Tier 3 - Advanced Skills (Require Tier 2)

        private static List<SkillDefinition> CreateTier3Skills()
        {
            int tier3Cost = (int)(BASE_SKILL_COST * TIER_COST_MULTIPLIER * TIER_COST_MULTIPLIER);
            
            return new List<SkillDefinition>
            {
                CreateSkill(
                    id: "berserker_fury",
                    name: "Berserker's Fury",
                    description: "Unleash devastating combat prowess",
                    tier: SkillTier.Advanced,
                    category: SkillCategory.Combat,
                    cost: tier3Cost,
                    effect: new CompositeSkillEffect(
                        "Berserker Mode: +30 Damage, +60% Attack Speed, +20% Speed",
                        SkillEffectType.Damage,
                        new DamageBoostEffect(30f),
                        new AttackSpeedEffect(1.6f),
                        new SpeedBoostEffect(1.2f)
                    ),
                    prerequisites: new[] { "combat_reflexes", "agility_master" },
                    position: new SkillPosition(2, 1)
                ),
                
                CreateSkill(
                    id: "fortress_defense",
                    name: "Fortress",
                    description: "Ultimate defensive stance",
                    tier: SkillTier.Advanced,
                    category: SkillCategory.Defense,
                    cost: tier3Cost + 10,
                    effect: new CompositeSkillEffect(
                        "Fortress Mode: +100 Health, -30% Damage Taken",
                        SkillEffectType.Defense,
                        new HealthBoostEffect(100f),
                        new ArmorEffect(0.3f)
                    ),
                    prerequisites: new[] { "iron_constitution", "tough_skin" },
                    position: new SkillPosition(2, 0)
                ),
                
                CreateSkill(
                    id: "master_scholar",
                    name: "Master Scholar",
                    description: "Peak learning efficiency and self-restoration",
                    tier: SkillTier.Advanced,
                    category: SkillCategory.Utility,
                    cost: tier3Cost,
                    effect: new CompositeSkillEffect(
                        "Master Scholar: +100% XP, +75 Health, +5 HP/sec Regen",
                        SkillEffectType.Utility,
                        new XPBoostEffect(2.0f),
                        new InstantHealEffect(75f),
                        new HealthRegenerationEffect(5f)
                    ),
                    prerequisites: new[] { "scholar_focus" },
                    position: new SkillPosition(2, 3)
                ),

                CreateSkill(
                    id: "weapon_mastery",
                    name: "Weapon Mastery",
                    description: "Master all aspects of combat",
                    tier: SkillTier.Advanced,
                    category: SkillCategory.Combat,
                    cost: tier3Cost,
                    effect: new CompositeSkillEffect(
                        "Weapon Master: +20 Damage, +2 Range, +15% Crit Chance",
                        SkillEffectType.Damage,
                        new DamageBoostEffect(20f),
                        new RangeBoostEffect(2f),
                        new CriticalHitEffect(0.15f)
                    ),
                    prerequisites: new[] { "reach_1", "sharp_strike_1" },
                    position: new SkillPosition(2, 2)
                )
            };
        }

        #endregion

        #region Tier 4 - Master Skills (Require multiple Tier 3)

        private static List<SkillDefinition> CreateTier4Skills()
        {
            int tier4Cost = (int)(BASE_SKILL_COST * TIER_COST_MULTIPLIER * TIER_COST_MULTIPLIER * 2f);
            
            return new List<SkillDefinition>
            {
                CreateSkill(
                    id: "champion_ascension",
                    name: "Champion's Ascension",
                    description: "Transcend mortal limitations",
                    tier: SkillTier.Master,
                    category: SkillCategory.Special,
                    cost: tier4Cost,
                    effect: new CompositeSkillEffect(
                        "Champion: Massive boost to all capabilities",
                        SkillEffectType.Utility,
                        new HealthBoostEffect(200f),
                        new DamageBoostEffect(50f),
                        new SpeedBoostEffect(1.5f),
                        new ArmorEffect(0.25f),
                        new AttackSpeedEffect(1.8f),
                        new XPBoostEffect(3.0f)
                    ),
                    prerequisites: new[] { "berserker_fury", "fortress_defense", "master_scholar" },
                    position: new SkillPosition(3, 1)
                ),

                CreateSkill(
                    id: "death_defiance",
                    name: "Death Defiance",
                    description: "Refuse to yield to death itself",
                    tier: SkillTier.Master,
                    category: SkillCategory.Defense,
                    cost: tier4Cost - 20,
                    effect: new CompositeSkillEffect(
                        "Death Defiance: Immunity on low health + massive regeneration",
                        SkillEffectType.Defense,
                        new ImmunityEffect(3f),
                        new HealthRegenerationEffect(15f),
                        new HealthBoostEffect(150f)
                    ),
                    prerequisites: new[] { "fortress_defense", "master_scholar" },
                    position: new SkillPosition(3, 0)
                ),

                CreateSkill(
                    id: "perfect_warrior",
                    name: "Perfect Warrior",
                    description: "Achieve combat perfection",
                    tier: SkillTier.Master,
                    category: SkillCategory.Combat,
                    cost: tier4Cost - 10,
                    effect: new CompositeSkillEffect(
                        "Perfect Warrior: Ultimate combat mastery",
                        SkillEffectType.Damage,
                        new DamageBoostEffect(75f),
                        new AttackSpeedEffect(2.5f),
                        new RangeBoostEffect(3f),
                        new CriticalHitEffect(0.5f) // 50% crit chance
                    ),
                    prerequisites: new[] { "berserker_fury", "weapon_mastery" },
                    position: new SkillPosition(3, 2)
                )
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a skill definition with all necessary properties
        /// </summary>
        private static SkillDefinition CreateSkill(
            string id,
            string name,
            string description,
            SkillTier tier,
            SkillCategory category,
            int cost,
            ISkillEffect effect,
            string[] prerequisites = null,
            SkillPosition position = null)
        {
            return new SkillDefinition
            {
                Id = id,
                Name = name,
                Description = description,
                Cost = cost,
                Effect = effect,
                Prerequisites = prerequisites?.ToList() ?? new List<string>(),
                Tier = tier,
                Category = category,
                Position = position ?? new SkillPosition(0, 0),
                IsUnlocked = false
            };
        }

        /// <summary>
        /// Validates that the skill tree has proper structure and no circular dependencies
        /// </summary>
        private static void ValidateSkillTree(List<SkillDefinition> skills)
        {
            var skillIds = skills.Select(s => s.Id).ToHashSet();
            
            foreach (var skill in skills)
            {
                // Validate all prerequisites exist
                foreach (var prerequisite in skill.Prerequisites)
                {
                    if (!skillIds.Contains(prerequisite))
                    {
                        throw new System.InvalidOperationException(
                            $"Skill '{skill.Id}' has invalid prerequisite '{prerequisite}' - skill does not exist");
                    }
                }
                
                // Check for self-reference
                if (skill.Prerequisites.Contains(skill.Id))
                {
                    throw new System.InvalidOperationException(
                        $"Skill '{skill.Id}' cannot be its own prerequisite");
                }
            }
            
            // Check for circular dependencies (basic check)
            DetectCircularDependencies(skills);
        }

        /// <summary>
        /// Detects circular dependencies in the skill tree
        /// </summary>
        private static void DetectCircularDependencies(List<SkillDefinition> skills)
        {
            var skillMap = skills.ToDictionary(s => s.Id, s => s);
            var visiting = new HashSet<string>();
            var visited = new HashSet<string>();

            foreach (var skill in skills)
            {
                if (!visited.Contains(skill.Id))
                {
                    CheckForCycles(skill.Id, skillMap, visiting, visited);
                }
            }
        }

        private static void CheckForCycles(string skillId, Dictionary<string, SkillDefinition> skillMap, 
            HashSet<string> visiting, HashSet<string> visited)
        {
            if (visiting.Contains(skillId))
            {
                throw new System.InvalidOperationException(
                    $"Circular dependency detected in skill tree involving skill '{skillId}'");
            }
            
            if (visited.Contains(skillId)) return;
            
            visiting.Add(skillId);
            
            if (skillMap.ContainsKey(skillId))
            {
                foreach (var prerequisite in skillMap[skillId].Prerequisites)
                {
                    CheckForCycles(prerequisite, skillMap, visiting, visited);
                }
            }
            
            visiting.Remove(skillId);
            visited.Add(skillId);
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Gets skills by category for UI organization
        /// </summary>
        public static List<SkillDefinition> GetSkillsByCategory(SkillCategory category)
        {
            return CreateAllSkills().Where(skill => skill.Category == category).ToList();
        }
        
        /// <summary>
        /// Gets skills by tier for progressive unlocking
        /// </summary>
        public static List<SkillDefinition> GetSkillsByTier(SkillTier tier)
        {
            return CreateAllSkills().Where(skill => skill.Tier == tier).ToList();
        }

        /// <summary>
        /// Gets all skills that have no prerequisites (entry points)
        /// </summary>
        public static List<SkillDefinition> GetRootSkills()
        {
            return CreateAllSkills().Where(skill => !skill.Prerequisites.Any()).ToList();
        }

        /// <summary>
        /// Gets all skills that depend on a specific skill
        /// </summary>
        public static List<SkillDefinition> GetDependentSkills(string skillId)
        {
            return CreateAllSkills().Where(skill => skill.Prerequisites.Contains(skillId)).ToList();
        }

        /// <summary>
        /// Gets total XP cost to unlock all skills
        /// </summary>
        public static int GetTotalSkillTreeCost()
        {
            return CreateAllSkills().Sum(skill => skill.Cost);
        }

        /// <summary>
        /// Creates a test skill for debugging and development
        /// </summary>
        public static SkillDefinition CreateTestSkill(string customName = null)
        {
            return CreateSkill(
                id: "test_skill",
                name: customName ?? "Test Skill",
                description: "A skill for testing purposes - grants small health boost",
                tier: SkillTier.Basic,
                category: SkillCategory.Utility,
                cost: 1,
                effect: new HealthBoostEffect(10f),
                position: new SkillPosition(0, 0)
            );
        }

        /// <summary>
        /// Creates a balanced skill set for different playstyles
        /// </summary>
        public static Dictionary<string, List<SkillDefinition>> GetSkillsByPlaystyle()
        {
            var allSkills = CreateAllSkills();
            
            return new Dictionary<string, List<SkillDefinition>>
            {
                ["Tank"] = allSkills.Where(s => s.Category == SkillCategory.Defense).ToList(),
                ["DPS"] = allSkills.Where(s => s.Category == SkillCategory.Combat).ToList(),
                ["Speed"] = allSkills.Where(s => s.Category == SkillCategory.Movement).ToList(),
                ["Support"] = allSkills.Where(s => s.Category == SkillCategory.Utility).ToList(),
                ["Hybrid"] = allSkills.Where(s => s.Effect is CompositeSkillEffect).ToList()
            };
        }

        #endregion
    }
}