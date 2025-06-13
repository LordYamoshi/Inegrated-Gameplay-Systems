using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Represents a skill button in the skill tree UI
    /// </summary>
    public class SkillButtonUI
    {
        // Unity references (assigned by GameManager)
        public Button Button { get; private set; }
        public TextMeshProUGUI NameText { get; private set; }
        public TextMeshProUGUI DescriptionText { get; private set; }
        public TextMeshProUGUI CostText { get; private set; }
        public Image IconImage { get; private set; }
        public Image BackgroundImage { get; private set; }
        public GameObject GameObject { get; private set; }
        
        // Visual state colors
        private readonly Color _availableColor = new Color(1f, 1f, 1f, 1f);      // White
        private readonly Color _unlockedColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        private readonly Color _lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);   // Gray
        private readonly Color _cannotAffordColor = new Color(0.8f, 0.3f, 0.3f, 1f); // Red
        private readonly Color _highlightColor = new Color(1f, 0.9f, 0.4f, 1f);  // Gold
        
        // Data and system references
        private SkillDefinition _skill;
        private ISkillSystem _skillSystem;
        private IPlayerSystem _playerSystem;
        private bool _isInitialized = false;
        
        // UI state tracking
        private SkillButtonState _currentState = SkillButtonState.Locked;
        private SkillButtonState _previousState = SkillButtonState.Locked;
        private float _stateTime = 0f;
        
        // Animation and effects
        private float _pulseTimer = 0f;
        private bool _shouldPulse = false;
        
        // Properties
        public SkillDefinition Skill => _skill;
        public bool IsInitialized => _isInitialized;
        public SkillButtonState CurrentState => _currentState;
        public bool CanBeUnlocked => _currentState == SkillButtonState.Available;

        /// <summary>
        /// Initialize the skill button with skill data and systems
        /// Called by GameManager during skill tree creation
        /// </summary>
        public void Initialize(SkillDefinition skill, ISkillSystem skillSystem, IPlayerSystem playerSystem, GameObject gameObject)
        {
            // Validate input parameters
            _skill = skill ?? throw new System.ArgumentNullException(nameof(skill));
            _skillSystem = skillSystem ?? throw new System.ArgumentNullException(nameof(skillSystem));
            _playerSystem = playerSystem ?? throw new System.ArgumentNullException(nameof(playerSystem));
            GameObject = gameObject ?? throw new System.ArgumentNullException(nameof(gameObject));
            
            // Get Unity UI components from the GameObject
            ExtractUIComponents();
            
            _isInitialized = true;
            
            // Setup UI content and initial state
            SetupUI();
            UpdateVisualState();
            
            Debug.Log($"SkillButtonUI: Initialized for skill '{skill.Name}'");
        }

        /// <summary>
        /// Update the button state - called by GameManager each frame
        /// </summary>
        public void Update()
        {
            if (!_isInitialized || _skill == null) return;
            
            _stateTime += Time.deltaTime;
            _pulseTimer += Time.deltaTime;
            
            // Update visual state if needed
            UpdateVisualState();
            
            // Handle animations and effects
            UpdateAnimations();
        }

        /// <summary>
        /// Handle button click - called by GameManager when Unity button is clicked
        /// </summary>
        public void OnButtonClicked()
        {
            if (!_isInitialized || _skill == null || _skillSystem == null)
            {
                Debug.LogWarning("SkillButtonUI: Button clicked but not properly initialized");
                return;
            }
            
            // Check if we can unlock the skill
            if (!_skillSystem.CanUnlockSkill(_skill))
            {
                Debug.Log($"Cannot unlock skill: {_skill.Name}");
                ShowCannotUnlockFeedback();
                return;
            }
            
            // Unlock the skill
            _skillSystem.UnlockSkill(_skill);
            
            Debug.Log($"Skill button clicked and unlocked: {_skill.Name}");
            
            // Show unlock feedback
            ShowUnlockFeedback();
        }

        /// <summary>
        /// Refresh the button's visual state - called when game state changes
        /// </summary>
        public void RefreshState()
        {
            if (_isInitialized)
            {
                UpdateVisualState(true); // Force update
            }
        }

        #region Private Methods

        private void ExtractUIComponents()
        {
            // Get main components
            Button = GameObject.GetComponent<Button>();
            BackgroundImage = GameObject.GetComponent<Image>();
            
            // Get text components from children
            var texts = GameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                switch (text.name.ToLower())
                {
                    case "nametext":
                    case "name":
                        NameText = text;
                        break;
                    case "descriptiontext":
                    case "description":
                        DescriptionText = text;
                        break;
                    case "costtext":
                    case "cost":
                        CostText = text;
                        break;
                }
            }
            
            // Get icon image if present
            var images = GameObject.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image.name.ToLower().Contains("icon") && image != BackgroundImage)
                {
                    IconImage = image;
                    break;
                }
            }
        }
        
        private void SetupUI()
        {
            if (_skill == null) return;

            // Set text content
            if (NameText != null)
                NameText.text = _skill.Name;
            
            if (DescriptionText != null)
                DescriptionText.text = _skill.Description;
            
            if (CostText != null)
                CostText.text = $"{_skill.Cost} XP";
            
            // Set icon if available and we have an icon component
            if (IconImage != null && !string.IsNullOrEmpty(_skill.IconPath))
            {
                // In a full implementation, load the icon from Resources or Addressables
                // For prototype, we'll use a placeholder or keep the default
            }
            
            // Set initial tooltip
            SetupTooltip();
        }

        private void UpdateVisualState(bool forceUpdate = false)
        {
            if (_skill == null || _skillSystem == null || _playerSystem == null) return;

            SkillButtonState newState = DetermineButtonState();
            
            if (newState != _currentState || forceUpdate)
            {
                _previousState = _currentState;
                _currentState = newState;
                _stateTime = 0f;
                
                ApplyVisualState(_currentState);
                UpdateInteractability(_currentState);
                
                // Start pulse animation for available skills
                _shouldPulse = (_currentState == SkillButtonState.Available);
            }
        }

        private SkillButtonState DetermineButtonState()
        {
            // Check if already unlocked
            if (_skillSystem.IsSkillUnlocked(_skill.Id))
            {
                return SkillButtonState.Unlocked;
            }
            
            // Check if prerequisites are met
            if (!_skill.ArePrerequisitesMet(_skillSystem))
            {
                return SkillButtonState.Locked;
            }
            
            // Check if player can afford it
            if (_playerSystem.XP < _skill.Cost)
            {
                return SkillButtonState.CannotAfford;
            }
            
            // Available to unlock
            return SkillButtonState.Available;
        }

        private void ApplyVisualState(SkillButtonState state)
        {
            Color targetColor = _lockedColor;
            float alpha = 0.5f;
            
            switch (state)
            {
                case SkillButtonState.Available:
                    targetColor = _availableColor;
                    alpha = 1f;
                    break;
                    
                case SkillButtonState.Unlocked:
                    targetColor = _unlockedColor;
                    alpha = 1f;
                    break;
                    
                case SkillButtonState.Locked:
                    targetColor = _lockedColor;
                    alpha = 0.5f;
                    break;
                    
                case SkillButtonState.CannotAfford:
                    targetColor = _cannotAffordColor;
                    alpha = 0.7f;
                    break;
            }
            
            // Apply color to background
            if (BackgroundImage != null)
            {
                var color = targetColor;
                color.a = alpha;
                BackgroundImage.color = color;
            }
            
            // Update text alpha
            UpdateTextAlpha(alpha);
        }

        private void UpdateTextAlpha(float alpha)
        {
            if (NameText != null)
            {
                var color = NameText.color;
                color.a = alpha;
                NameText.color = color;
            }
            
            if (DescriptionText != null)
            {
                var color = DescriptionText.color;
                color.a = alpha * 0.8f; // Slightly more transparent for description
                DescriptionText.color = color;
            }
            
            if (CostText != null)
            {
                var color = CostText.color;
                color.a = alpha;
                CostText.color = color;
            }
        }

        private void UpdateInteractability(SkillButtonState state)
        {
            if (Button != null)
            {
                bool interactable = state == SkillButtonState.Available;
                Button.interactable = interactable;
            }
        }

        private void UpdateAnimations()
        {
            // Pulse animation for available skills
            if (_shouldPulse && BackgroundImage != null)
            {
                float pulseIntensity = (Mathf.Sin(_pulseTimer * 3f) + 1f) * 0.1f; // Subtle pulse
                var currentColor = BackgroundImage.color;
                
                // Brighten the color slightly
                Color pulseColor = Color.Lerp(currentColor, _highlightColor, pulseIntensity);
                BackgroundImage.color = pulseColor;
            }
        }

        private void ShowUnlockFeedback()
        {
            // In a full implementation, this could trigger:
            // - Particle effects
            // - Sound effects  
            // - Scale animation
            // - Screen shake
            
            Debug.Log($"Visual feedback: Skill unlocked - {_skill.Name}!");
            
            // Simple scale effect (would be better with DOTween or similar)
            if (GameObject != null)
            {
                // Could add a simple scale animation here
            }
        }

        private void ShowCannotUnlockFeedback()
        {
            // In a full implementation, this could show:
            // - Error message popup
            // - Shake animation
            // - Error sound
            // - Red flash effect
            
            Debug.Log($"Cannot unlock feedback: {_skill.Name}");
            
            // Simple shake effect indicator
            if (BackgroundImage != null)
            {
                // Flash red briefly
                var originalColor = BackgroundImage.color;
                BackgroundImage.color = _cannotAffordColor;
                // Would reset color after a short delay in full implementation
            }
        }

        private void SetupTooltip()
        {
            // In a full implementation, this would setup Unity's tooltip system
            // For now, we'll prepare the tooltip text
        }

        private void ShowTooltip()
        {
            if (_skill == null) return;
            
            string tooltipText = GetTooltipText();
            Debug.Log($"Tooltip: {tooltipText}");
            
            // In a full implementation, this would show an actual tooltip UI
        }

        private void HideTooltip()
        {
            // Hide tooltip in full implementation
        }

        private string GetTooltipText()
        {
            if (_skill == null) return "";
            
            var tooltip = $"<b>{_skill.Name}</b>\n\n";
            tooltip += $"{_skill.Description}\n\n";
            tooltip += $"<b>Effect:</b> {_skill.Effect?.GetDescription() ?? "No effect"}\n";
            tooltip += $"<b>Cost:</b> {_skill.Cost} XP\n";
            tooltip += $"<b>Category:</b> {_skill.Category}\n";
            tooltip += $"<b>Tier:</b> {_skill.Tier}\n";
            
            if (_skill.Prerequisites.Count > 0)
            {
                tooltip += $"\n<b>Prerequisites:</b>\n";
                foreach (var prereq in _skill.Prerequisites)
                {
                    var prereqSkill = _skillSystem.GetSkillById(prereq);
                    string prereqName = prereqSkill?.Name ?? prereq;
                    bool isUnlocked = _skillSystem.IsSkillUnlocked(prereq);
                    string status = isUnlocked ? "✓" : "✗";
                    Color statusColor = isUnlocked ? Color.green : Color.red;
                    tooltip += $"  <color=#{ColorUtility.ToHtmlStringRGB(statusColor)}>{status}</color> {prereqName}\n";
                }
            }
            
            // Add current player info
            tooltip += $"\n<b>Your XP:</b> {_playerSystem.XP}";
            
            if (_currentState == SkillButtonState.CannotAfford)
            {
                int xpNeeded = _skill.Cost - _playerSystem.XP;
                tooltip += $"\n<color=red>Need {xpNeeded} more XP</color>";
            }
            else if (_currentState == SkillButtonState.Available)
            {
                tooltip += $"\n<color=green>Click to unlock!</color>";
            }
            
            return tooltip;
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Get detailed button information for debugging
        /// </summary>
        public SkillButtonInfo GetButtonInfo()
        {
            return new SkillButtonInfo
            {
                SkillId = _skill?.Id ?? "null",
                SkillName = _skill?.Name ?? "null",
                CurrentState = _currentState,
                IsInitialized = _isInitialized,
                CanBeUnlocked = CanBeUnlocked,
                PlayerXP = _playerSystem?.XP ?? 0,
                SkillCost = _skill?.Cost ?? 0,
                PrerequisitesMet = _skill?.ArePrerequisitesMet(_skillSystem) ?? false
            };
        }

        /// <summary>
        /// Force a specific visual state (for testing)
        /// </summary>
        public void ForceState(SkillButtonState state)
        {
            _currentState = state;
            ApplyVisualState(state);
            UpdateInteractability(state);
        }

        /// <summary>
        /// Get the unlock path for this skill
        /// </summary>
        public List<SkillDefinition> GetUnlockPath()
        {
            return _skillSystem?.GetUnlockPath(_skill.Id) ?? new List<SkillDefinition>();
        }

        /// <summary>
        /// Get total cost to unlock this skill including prerequisites
        /// </summary>
        public int GetTotalUnlockCost()
        {
            return _skillSystem?.GetTotalUnlockCost(_skill.Id) ?? 0;
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Print detailed button state information
        /// </summary>
        public void PrintDebugInfo()
        {
            var info = GetButtonInfo();
            
            Debug.Log($"=== SKILL BUTTON DEBUG: {info.SkillName} ===");
            Debug.Log($"State: {info.CurrentState}");
            Debug.Log($"Initialized: {info.IsInitialized}");
            Debug.Log($"Can Unlock: {info.CanBeUnlocked}");
            Debug.Log($"Player XP: {info.PlayerXP}");
            Debug.Log($"Skill Cost: {info.SkillCost}");
            Debug.Log($"Prerequisites Met: {info.PrerequisitesMet}");
            Debug.Log($"Components Valid: Button={Button != null}, NameText={NameText != null}, Background={BackgroundImage != null}");
        }

        #endregion
    }

    #region Supporting Enums and Classes

    /// <summary>
    /// Visual states for skill buttons
    /// </summary>
    public enum SkillButtonState
    {
        Available,      // Can be unlocked (green/highlighted)
        Unlocked,       // Already unlocked (green/completed)
        Locked,         // Prerequisites not met (gray/disabled)
        CannotAfford    // Not enough XP (red/expensive)
    }

    /// <summary>
    /// Debug information structure for skill buttons
    /// </summary>
    public class SkillButtonInfo
    {
        public string SkillId { get; set; }
        public string SkillName { get; set; }
        public SkillButtonState CurrentState { get; set; }
        public bool IsInitialized { get; set; }
        public bool CanBeUnlocked { get; set; }
        public int PlayerXP { get; set; }
        public int SkillCost { get; set; }
        public bool PrerequisitesMet { get; set; }
    }

    #endregion
}