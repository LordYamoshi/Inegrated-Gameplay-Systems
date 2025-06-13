using UnityEngine;
using System.Collections.Generic;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Manages the user interface system, including health, XP, waves, and game over screens
    /// </summary>
    public class UISystem : IUISystem
    {
        private readonly IEventBus _eventBus;
        
        // UI State tracking
        private float _currentHealth = 100f;
        private float _maxHealth = 100f;
        private int _currentXP = 0;
        private int _currentWave = 1;
        private bool _gameOverShown = false;
        private readonly Dictionary<string, object> _uiState = new Dictionary<string, object>();
        
        // UI Update tracking to prevent unnecessary updates
        private readonly Dictionary<string, object> _lastValues = new Dictionary<string, object>();
        
        public UISystem(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
            Initialize(_eventBus);
        }

        public void Initialize(IEventBus eventBus)
        {
            // Subscribe to all UI-relevant events
            _eventBus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            _eventBus.Subscribe<PlayerXPChangedEvent>(OnPlayerXPChanged);
            _eventBus.Subscribe<WaveChangedEvent>(OnWaveChanged);
            _eventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            _eventBus.Subscribe<GameOverEvent>(OnGameOver);
            _eventBus.Subscribe<SkillUnlockedEvent>(OnSkillUnlocked);
            _eventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            _eventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            
            Debug.Log("UISystem initialized and subscribed to events");
            
            // Initialize UI state
            InitializeUIState();
            ForceRefreshAll();
        }

        public void UpdateHealthDisplay(float current, float max)
        {
            if (HasValueChanged("Health", current) || HasValueChanged("MaxHealth", max))
            {
                _currentHealth = current;
                _maxHealth = max;
                
                // Request UI update through event
                _eventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "HealthDisplay",
                    Data = new Dictionary<string, object>
                    {
                        ["CurrentHealth"] = current,
                        ["MaxHealth"] = max,
                        ["HealthPercentage"] = max > 0 ? current / max : 0f,
                        ["IsCritical"] = (current / max) <= 0.25f,
                        ["DisplayText"] = $"{current:F0}/{max:F0}"
                    }
                });
                
                UpdateLastValue("Health", current);
                UpdateLastValue("MaxHealth", max);
                
                Debug.Log($"UI: Health display updated - {current:F1}/{max:F1}");
            }
        }

        public void UpdateXPDisplay(int xp)
        {
            if (HasValueChanged("XP", xp))
            {
                _currentXP = xp;
                
                _eventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "XPDisplay",
                    Data = new Dictionary<string, object>
                    {
                        ["CurrentXP"] = xp,
                        ["DisplayText"] = $"XP: {xp}",
                        ["XPGained"] = xp - (int)_lastValues.GetValueOrDefault("XP", 0)
                    }
                });
                
                UpdateLastValue("XP", xp);
                
                Debug.Log($"UI: XP display updated - {xp}");
            }
        }

        public void UpdateWaveDisplay(int wave)
        {
            if (HasValueChanged("Wave", wave))
            {
                _currentWave = wave;
                
                _eventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "WaveDisplay",
                    Data = new Dictionary<string, object>
                    {
                        ["CurrentWave"] = wave,
                        ["DisplayText"] = $"Wave: {wave}",
                        ["IsNewWave"] = wave > (int)_lastValues.GetValueOrDefault("Wave", 0)
                    }
                });
                
                UpdateLastValue("Wave", wave);
                
                Debug.Log($"UI: Wave display updated - {wave}");
            }
        }

        public void ShowGameOver(bool victory, int finalWave, int totalXP)
        {
            if (!_gameOverShown)
            {
                _gameOverShown = true;
                
                string message = victory ? 
                    $"Victory!\nSurvived {finalWave} waves!" : 
                    $"Game Over!\nReached Wave {finalWave}";
                
                _eventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "GameOverScreen",
                    Data = new Dictionary<string, object>
                    {
                        ["Show"] = true,
                        ["Victory"] = victory,
                        ["Message"] = message,
                        ["FinalWave"] = finalWave,
                        ["TotalXP"] = totalXP,
                        ["ShowRestartButton"] = true
                    }
                });
                
                Debug.Log($"UI: Game over screen shown - Victory: {victory}");
            }
        }

        public void ShowSkillUnlocked(SkillDefinition skill)
        {
            if (skill == null) return;
            
            _eventBus.Publish(new UpdateUIEvent
            {
                UIElement = "SkillUnlockedNotification",
                Data = new Dictionary<string, object>
                {
                    ["SkillName"] = skill.Name,
                    ["SkillDescription"] = skill.Description,
                    ["SkillEffect"] = skill.Effect?.GetDescription() ?? "No effect",
                    ["Duration"] = 3f // Show for 3 seconds
                }
            });
            
            // Also trigger visual effects
            _eventBus.Publish(new VisualEffectEvent
            {
                EffectType = "SkillUnlocked",
                Position = Vector3.zero,
                Duration = 2f,
                Parameters = new Dictionary<string, object>
                {
                    ["SkillName"] = skill.Name,
                    ["SkillTier"] = skill.Tier.ToString()
                }
            });
            
            Debug.Log($"UI: Skill unlocked notification - {skill.Name}");
        }

        #region Private Event Handlers

        private void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
        {
            UpdateHealthDisplay(evt.CurrentHealth, evt.MaxHealth);
            
            // Handle critical health warning
            if (evt.IsCriticalHealth && !evt.IsHealing)
            {
                _eventBus.Publish(new VisualEffectEvent
                {
                    EffectType = "CriticalHealthWarning",
                    Position = Vector3.zero,
                    Duration = 1f
                });
            }
            
            // Handle healing effects
            if (evt.IsHealing)
            {
                _eventBus.Publish(new VisualEffectEvent
                {
                    EffectType = "HealthRestored",
                    Position = Vector3.zero,
                    Duration = 0.5f
                });
            }
        }

        private void OnPlayerXPChanged(PlayerXPChangedEvent evt)
        {
            UpdateXPDisplay(evt.CurrentXP);
            
            // Show XP gain effect if XP was gained
            if (evt.XPGained > 0)
            {
                _eventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "XPGainedEffect",
                    Data = new Dictionary<string, object>
                    {
                        ["XPGained"] = evt.XPGained,
                        ["Source"] = evt.Source,
                        ["DisplayText"] = $"+{evt.XPGained} XP"
                    }
                });
            }
        }

        private void OnWaveChanged(WaveChangedEvent evt)
        {
            UpdateWaveDisplay(evt.WaveNumber);
            
            // Show wave start notification
            _eventBus.Publish(new UpdateUIEvent
            {
                UIElement = "WaveStartNotification",
                Data = new Dictionary<string, object>
                {
                    ["WaveNumber"] = evt.WaveNumber,
                    ["EnemyCount"] = evt.EnemiesInWave,
                    ["Difficulty"] = evt.WaveDifficulty,
                    ["Message"] = $"Wave {evt.WaveNumber} - {evt.EnemiesInWave} enemies incoming!"
                }
            });
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            // Show wave completion notification
            _eventBus.Publish(new UpdateUIEvent
            {
                UIElement = "WaveCompletedNotification",
                Data = new Dictionary<string, object>
                {
                    ["CompletedWave"] = evt.CompletedWave,
                    ["XPEarned"] = evt.TotalXPEarned,
                    ["CompletionTime"] = evt.WaveCompletionTime,
                    ["Message"] = $"Wave {evt.CompletedWave} Complete!",
                    ["NextWaveDelay"] = evt.DelayBeforeNextWave
                }
            });
            
            // Trigger celebration effect
            _eventBus.Publish(new VisualEffectEvent
            {
                EffectType = "WaveComplete",
                Position = Vector3.zero,
                Duration = 2f
            });
        }

        private void OnGameOver(GameOverEvent evt)
        {
            ShowGameOver(evt.Victory, evt.FinalWave, evt.TotalXP);
            
            // Show detailed statistics
            _eventBus.Publish(new UpdateUIEvent
            {
                UIElement = "GameStatistics",
                Data = new Dictionary<string, object>
                {
                    ["FinalWave"] = evt.FinalWave,
                    ["TotalXP"] = evt.TotalXP,
                    ["SkillsUnlocked"] = evt.SkillsUnlocked,
                    ["SurvivalTime"] = evt.TotalSurvivalTime,
                    ["Statistics"] = evt.Statistics
                }
            });
        }

        private void OnSkillUnlocked(SkillUnlockedEvent evt)
        {
            ShowSkillUnlocked(evt.Skill);
            
            // Update skill tree UI state
            _eventBus.Publish(new UpdateUIEvent
            {
                UIElement = "SkillTreeRefresh",
                Data = new Dictionary<string, object>
                {
                    ["UnlockedSkillId"] = evt.Skill.Id,
                    ["RemainingXP"] = evt.RemainingXP,
                    ["TotalSkillsUnlocked"] = evt.TotalSkillsUnlocked
                }
            });
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            // Show damage taken effect
            _eventBus.Publish(new UpdateUIEvent
            {
                UIElement = "DamageTakenEffect",
                Data = new Dictionary<string, object>
                {
                    ["Damage"] = evt.ActualDamage,
                    ["DamageType"] = evt.DamageType,
                    ["IsCritical"] = evt.IsCriticalHit,
                    ["DamageReduction"] = evt.DamageReduction
                }
            });
            
            // Screen shake for significant damage
            if (evt.ActualDamage > 25f)
            {
                _eventBus.Publish(new VisualEffectEvent
                {
                    EffectType = "ScreenShake",
                    Position = Vector3.zero,
                    Duration = 0.3f,
                    Parameters = new Dictionary<string, object>
                    {
                        ["Intensity"] = evt.ActualDamage / 100f
                    }
                });
            }
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            // Show enemy defeated effect
            _eventBus.Publish(new VisualEffectEvent
            {
                EffectType = "EnemyDefeated",
                Position = evt.Position,
                Duration = 1f,
                Parameters = new Dictionary<string, object>
                {
                    ["EnemyType"] = evt.EnemyType,
                    ["XPReward"] = evt.XPReward
                }
            });
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            // Handle UI changes based on state transitions
            _eventBus.Publish(new UpdateUIEvent
            {
                UIElement = "GameStateUI",
                Data = new Dictionary<string, object>
                {
                    ["NewState"] = evt.NewState,
                    ["PreviousState"] = evt.PreviousState,
                    ["StateData"] = evt.StateData
                }
            });
        }

        #endregion

        #region Private Helper Methods

        private void InitializeUIState()
        {
            _uiState["Health"] = _currentHealth;
            _uiState["MaxHealth"] = _maxHealth;
            _uiState["XP"] = _currentXP;
            _uiState["Wave"] = _currentWave;
            _uiState["GameOverShown"] = _gameOverShown;
            
            // Initialize last values
            foreach (var kvp in _uiState)
            {
                _lastValues[kvp.Key] = kvp.Value;
            }
        }

        private bool HasValueChanged(string key, object newValue)
        {
            if (!_lastValues.ContainsKey(key))
            {
                return true;
            }
            
            var lastValue = _lastValues[key];
            return !Equals(lastValue, newValue);
        }

        private void UpdateLastValue(string key, object value)
        {
            _lastValues[key] = value;
            _uiState[key] = value;
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Get current UI state for debugging
        /// </summary>
        public Dictionary<string, object> GetUIState()
        {
            return new Dictionary<string, object>(_uiState);
        }

        /// <summary>
        /// Reset UI state (for testing)
        /// </summary>
        public void ResetUIState()
        {
            _currentHealth = 100f;
            _maxHealth = 100f;
            _currentXP = 0;
            _currentWave = 1;
            _gameOverShown = false;
            
            _lastValues.Clear();
            _uiState.Clear();
            
            InitializeUIState();
            
            Debug.Log("UI state reset");
        }

        /// <summary>
        /// Force refresh all UI elements
        /// </summary>
        public void ForceRefreshAll()
        {
            _lastValues.Clear(); // This will force all updates on next call
            
            UpdateHealthDisplay(_currentHealth, _maxHealth);
            UpdateXPDisplay(_currentXP);
            UpdateWaveDisplay(_currentWave);
            
            Debug.Log("Forced UI refresh");
        }

        /// <summary>
        /// Show damage number at position
        /// </summary>
        public void ShowDamageNumber(Vector3 position, float damage, bool isCritical = false)
        {
            _eventBus.Publish(new UpdateUIEvent
            {
                UIElement = "DamageNumber",
                Data = new Dictionary<string, object>
                {
                    ["Position"] = position,
                    ["Damage"] = damage,
                    ["IsCritical"] = isCritical,
                    ["DisplayText"] = $"-{damage:F0}"
                }
            });
        }

        /// <summary>
        /// Update skill tree availability indicators
        /// </summary>
        public void UpdateSkillTreeIndicators(int availableSkills, int playerXP)
        {
            _eventBus.Publish(new UpdateUIEvent
            {
                UIElement = "SkillTreeButton",
                Data = new Dictionary<string, object>
                {
                    ["AvailableSkills"] = availableSkills,
                    ["PlayerXP"] = playerXP,
                    ["HasAvailableSkills"] = availableSkills > 0,
                    ["ButtonText"] = availableSkills > 0 ? $"Skills ({availableSkills})" : "Skills"
                }
            });
        }

        #endregion
    }
}