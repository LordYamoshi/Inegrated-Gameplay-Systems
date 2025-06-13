using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace SkillTreeSurvivor
{

    /// <summary>
    /// Manages the overall game state, player, enemies, and UI
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private Canvas _gameUI;

        [SerializeField] private Canvas _skillTreeUI;
        [SerializeField] private Button _skillTreeButton;
        [SerializeField] private Button _closeSkillTreeButton;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _xpText;
        [SerializeField] private TextMeshProUGUI _waveText;
        [SerializeField] private TextMeshProUGUI _gameOverText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Slider _healthBar;

        [Header("Game Objects")] [SerializeField]
        private Transform _playerTransform;

        [SerializeField] private Transform _enemySpawnParent;
        [SerializeField] private GameObject _enemyPrefab;

        [Header("Skill Tree")] [SerializeField]
        private Transform _skillTreeParent;

        [SerializeField] private GameObject _skillButtonPrefab;

        [Header("Game Configuration")] [SerializeField]
        private bool _autoStartFirstWave = true;

        [SerializeField] private float _firstWaveDelay = 2f;

        // Game Systems (Composition over Inheritance)
        private IGameStateMachine _gameStateMachine;
        private IPlayerSystem _playerSystem;
        private ISkillSystem _skillSystem;
        private IEnemySystem _enemySystem;
        private IUISystem _uiSystem;
        private IEventBus _eventBus;

        // System References for cross-system communication
        private PlayerSystem _playerSystemImpl;
        private SkillSystem _skillSystemImpl;
        private EnemySystem _enemySystemImpl;

        // Skill tree UI components (pure C# objects, not MonoBehaviours)
        private List<SkillButtonUI> _skillButtons = new List<SkillButtonUI>();

        // Enemy management (pure C# objects, not MonoBehaviours)
        private List<Enemy> _activeEnemies = new List<Enemy>();

        // Game state
        private bool _gameInitialized = false;
        private float _waveStartTimer = 0f;

        #region Unity Lifecycle

        private void Awake()
        {

            InitializeSystems();
            SetupEventListeners();
            SetupUI();

            _gameInitialized = true;
            Debug.Log("GameManager: Initialization complete");
        }

        private void Start()
        {
            if (!_gameInitialized) return;

            _gameStateMachine.ChangeState(GameStateType.Playing);

            // Start first wave after delay
            if (_autoStartFirstWave)
            {
                _waveStartTimer = _firstWaveDelay;
            }

            Debug.Log("GameManager: Game started");
        }

        private void Update()
        {
            if (!_gameInitialized) return;

            // Update all systems
            _gameStateMachine.Update();
            _playerSystem.Update();
            _enemySystem.Update();
            _skillSystem.Update();

            // Update all active enemies (since they're pure C# classes now)
            UpdateEnemies();

            // Update skill buttons
            UpdateSkillButtons();

            // Handle player movement
            HandlePlayerMovement();

            // Handle player combat
            HandlePlayerCombat();

            // Handle wave start timer
            if (_waveStartTimer > 0)
            {
                _waveStartTimer -= Time.deltaTime;
                if (_waveStartTimer <= 0)
                {
                    _enemySystem.SpawnWave(1);
                }
            }

            // Handle debug input
            HandleDebugInput();
        }

        private void OnDestroy()
        {
            // Cleanup
            _eventBus?.UnsubscribeAll();
            Debug.Log("GameManager: Cleanup complete");
        }

        #endregion

        #region System Initialization

        private void InitializeSystems()
        {
            // Initialize Event Bus first (other systems depend on it)
            _eventBus = new EventBus();

            // Configure player system
            var playerConfig = new PlayerConfig
            {
                Transform = _playerTransform,
                MaxHealth = 100f,
                Speed = 5f,
                AttackDamage = 25f,
                AttackRange = 3f,
                AttackRate = 1f
            };

            // Initialize core systems with dependency injection
            _playerSystemImpl = new PlayerSystem(playerConfig, _eventBus);
            _playerSystem = _playerSystemImpl;

            _skillSystemImpl = new SkillSystem(_eventBus, _playerSystem);
            _skillSystem = _skillSystemImpl;

            _enemySystemImpl = new EnemySystem(_enemyPrefab, _enemySpawnParent, _eventBus);
            _enemySystem = _enemySystemImpl;

            _uiSystem = new UISystem(_eventBus);
            _gameStateMachine = new GameStateMachine(_eventBus);

            // Initialize skill tree UI
            InitializeSkillTree();

            Debug.Log("GameManager: All systems initialized");
        }

        private void SetupEventListeners()
        {
            // Subscribe to events that require GameManager handling
            _eventBus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            _eventBus.Subscribe<PlayerXPChangedEvent>(OnPlayerXPChanged);
            _eventBus.Subscribe<WaveChangedEvent>(OnWaveChanged);
            _eventBus.Subscribe<GameOverEvent>(OnGameOver);
            _eventBus.Subscribe<SkillUnlockedEvent>(OnSkillUnlocked);
            _eventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            _eventBus.Subscribe<CreateEnemyEvent>(OnCreateEnemyRequested);
            _eventBus.Subscribe<PlayerAttackEvent>(OnPlayerAttack);

            Debug.Log("GameManager: Event listeners setup complete");
        }

        private void SetupUI()
        {
            // Setup skill tree button
            if (_skillTreeButton != null)
            {
                _skillTreeButton.onClick.AddListener(OpenSkillTree);
            }

            if (_closeSkillTreeButton != null)
            {
                _closeSkillTreeButton.onClick.AddListener(CloseSkillTree);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(RestartGame);
            }

            // Initialize UI state
            if (_skillTreeUI != null)
                _skillTreeUI.gameObject.SetActive(false);

            if (_gameOverText != null)
                _gameOverText.gameObject.SetActive(false);

            if (_restartButton != null)
                _restartButton.gameObject.SetActive(false);

            // Initialize health bar
            if (_healthBar != null)
            {
                _healthBar.maxValue = 100f;
                _healthBar.value = 100f;
            }

            Debug.Log("GameManager: UI setup complete");
        }

        private void InitializeSkillTree()
        {
            var skillDefinitions = SkillFactory.CreateAllSkills();

            Debug.Log($"GameManager: Creating {skillDefinitions.Count} skill buttons");

            foreach (var skill in skillDefinitions)
            {
                CreateSkillButton(skill);
            }

            Debug.Log("GameManager: Skill tree initialization complete");
        }

        private void CreateSkillButton(SkillDefinition skill)
        {
            if (_skillButtonPrefab == null || _skillTreeParent == null) return;

            var skillButtonObj = Instantiate(_skillButtonPrefab, _skillTreeParent);

            // Create pure C# SkillButtonUI object (not MonoBehaviour)
            var skillButtonUI = new SkillButtonUI();
            skillButtonUI.Initialize(skill, _skillSystem, _playerSystem, skillButtonObj);

            // Set up button click listener to call our pure C# object
            var button = skillButtonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => skillButtonUI.OnButtonClicked());
            }

            _skillButtons.Add(skillButtonUI);

            // Position the button based on skill position data
            PositionSkillButton(skillButtonObj.transform, skill.Position);
        }

        private void PositionSkillButton(Transform buttonTransform, SkillPosition position)
        {
            if (buttonTransform == null) return;

            // Simple grid layout for prototype
            float spacing = 150f;
            float offsetX = position.Column * spacing;
            float offsetY = -position.Row * spacing;

            var rectTransform = buttonTransform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(offsetX, offsetY);
            }
        }

        #endregion

        #region Game Loop Management

        private void HandlePlayerMovement()
        {
            if (_playerSystem == null || _playerTransform == null) return;

            Vector3 movementInput = _playerSystem.GetMovementInput();
            if (movementInput.magnitude > 0.1f)
            {
                Vector3 movement = movementInput * _playerSystem.Speed * Time.deltaTime;
                _playerTransform.position += movement;

                // Face movement direction
                _playerTransform.rotation = Quaternion.LookRotation(movementInput);
            }
        }

        private void HandlePlayerCombat()
        {
            if (_playerSystem == null) return;

            if (_playerSystem.ShouldAttack(out float attackDamage, out float attackRange))
            {
                // Find enemies within range and attack them
                var nearbyEnemies = FindEnemiesInRange(_playerSystem.Position, attackRange);
                if (nearbyEnemies.Count > 0)
                {
                    foreach (var enemy in nearbyEnemies)
                    {
                        enemy.TakeDamage(attackDamage);
                    }

                    _playerSystem.OnAttackPerformed();
                }
            }
        }

        private List<Enemy> FindEnemiesInRange(Vector3 position, float range)
        {
            var enemiesInRange = new List<Enemy>();

            foreach (var enemy in _activeEnemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    float distance = Vector3.Distance(position, enemy.Position);
                    if (distance <= range)
                    {
                        enemiesInRange.Add(enemy);
                    }
                }
            }

            return enemiesInRange;
        }

        /// <summary>
        /// Update all active enemies - called from main Update loop
        /// </summary>
        private void UpdateEnemies()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];

                if (enemy == null || enemy.IsDead || enemy.GameObject == null)
                {
                    // Clean up dead or null enemies
                    if (enemy?.GameObject != null)
                    {
                        Destroy(enemy.GameObject);
                    }

                    _activeEnemies.RemoveAt(i);
                    continue;
                }

                // Update enemy AI
                enemy.Update();

                // Handle enemy movement
                ApplyEnemyMovement(enemy);

                // Handle enemy attacks
                if (enemy.ShouldAttackPlayer())
                {
                    enemy.PerformAttack();
                }
            }
        }

        /// <summary>
        /// Apply movement to enemy GameObject
        /// </summary>
        private void ApplyEnemyMovement(Enemy enemy)
        {
            if (enemy?.GameObject == null) return;

            var rigidbody = enemy.GameObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                Vector3 movementVector = enemy.GetMovementVector();

                // Apply horizontal movement while preserving gravity (Y velocity)
                Vector3 targetVelocity = new Vector3(movementVector.x, rigidbody.linearVelocity.y, movementVector.z);
                rigidbody.linearVelocity = targetVelocity;

                // Face movement direction (only horizontal)
                if (movementVector.magnitude > 0.1f)
                {
                    Vector3 lookDirection = new Vector3(movementVector.x, 0, movementVector.z);
                    if (lookDirection.magnitude > 0.1f)
                    {
                        enemy.Transform.rotation = Quaternion.LookRotation(lookDirection);
                    }
                }
            }
            else
            {
                Vector3 movementVector = enemy.GetMovementVector();
                Vector3 horizontalMovement = new Vector3(movementVector.x, 0, movementVector.z);
                enemy.Transform.position += horizontalMovement * Time.deltaTime;
            }
        }

        /// <summary>
        /// Update all skill buttons - called from main Update loop
        /// </summary>
        private void UpdateSkillButtons()
        {
            foreach (var skillButton in _skillButtons)
            {
                if (skillButton != null && skillButton.IsInitialized)
                {
                    skillButton.Update();
                }
            }
        }

        /// <summary>
        /// Create a new enemy - called by EnemySystem through events
        /// </summary>
        public void CreateEnemy(Vector3 position, float health, float damage)
        {
            if (_enemyPrefab == null) return;

            // Use enemy spawn parent's Y position if available, otherwise spawn higher for falling
            Vector3 spawnPosition = position;
            if (_enemySpawnParent != null)
            {
                spawnPosition.y = _enemySpawnParent.position.y + 2f; // Spawn 2 units above spawn parent
            }
            else
            {
                spawnPosition.y = 2f; // Default spawn height
            }

            // Create GameObject
            var enemyObj = Instantiate(_enemyPrefab, spawnPosition, Quaternion.identity, _enemySpawnParent);

            // Ensure it has required components
            var collider = enemyObj.GetComponent<Collider>();
            if (collider == null)
            {
                collider = enemyObj.AddComponent<CapsuleCollider>();
                collider.isTrigger = false;
            }

            var rigidbody = enemyObj.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = enemyObj.AddComponent<Rigidbody>();
            }

            // Configure rigidbody for top-down movement
            // Only freeze rotation, allow Y movement for gravity and falling
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            // Set physics properties
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 2f; // Add some drag to prevent sliding
            rigidbody.angularDamping = 5f;
            rigidbody.useGravity = true; // Ensure gravity is enabled

            // Create Enemy object
            var enemy = new Enemy();
            enemy.Initialize(health, damage, enemyObj, _playerTransform, _eventBus);

            _activeEnemies.Add(enemy);

            Debug.Log($"GameManager created enemy at {spawnPosition}");
        }

        #endregion

        #region Event Handlers

        private void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
        {
            // Update UI
            if (_healthText != null)
            {
                _healthText.text = $"Health: {evt.CurrentHealth:F0}/{evt.MaxHealth:F0}";
            }

            if (_healthBar != null)
            {
                _healthBar.maxValue = evt.MaxHealth;
                _healthBar.value = evt.CurrentHealth;
            }
        }

        private void OnPlayerXPChanged(PlayerXPChangedEvent evt)
        {
            // Update UI
            if (_xpText != null)
            {
                _xpText.text = $"XP: {evt.CurrentXP}";
            }

            // Refresh skill buttons to update availability
            RefreshSkillButtons();
        }

        private void OnWaveChanged(WaveChangedEvent evt)
        {
            // Update UI
            if (_waveText != null)
            {
                _waveText.text = $"Wave: {evt.WaveNumber}";
            }
        }

        private void OnGameOver(GameOverEvent evt)
        {
            // Update UI
            if (_gameOverText != null)
            {
                _gameOverText.gameObject.SetActive(true);
                _gameOverText.text = evt.Victory
                    ? $"Victory!\nSurvived {evt.FinalWave} waves!"
                    : $"Game Over!\nReached Wave {evt.FinalWave}";
            }

            if (_restartButton != null)
            {
                _restartButton.gameObject.SetActive(true);
            }

            // Change to appropriate game state
            var targetState = evt.Victory ? GameStateType.Victory : GameStateType.GameOver;
            _gameStateMachine.ChangeState(targetState);

            Time.timeScale = 0f;
        }

        private void OnSkillUnlocked(SkillUnlockedEvent evt)
        {
            Debug.Log($"GameManager: Skill unlocked - {evt.Skill.Name}");

            // Refresh all skill buttons
            RefreshSkillButtons();

            // Could add visual feedback here
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            // Player damage is already handled by the PlayerSystem
            // This is just for additional effects like screen shake, sound, etc.
            Debug.Log($"GameManager: Player took {evt.Damage} damage from {evt.DamageType}");
        }

        private void OnCreateEnemyRequested(CreateEnemyEvent evt)
        {
            // Create enemy as requested by EnemySystem
            CreateEnemy(evt.Position, evt.Health, evt.Damage);
        }

        private void OnPlayerAttack(PlayerAttackEvent evt)
        {
            // Handle visual/audio feedback for player attacks
            Debug.Log($"Player attacked at {evt.AttackPosition} for {evt.Damage} damage");
        }

        #endregion

        #region UI Methods

        private void OpenSkillTree()
        {
            if (_skillTreeUI != null)
            {
                _skillTreeUI.gameObject.SetActive(true);
                Time.timeScale = 0f;

                // Refresh skill buttons when opening
                RefreshSkillButtons();

                _eventBus.Publish(new SkillTreeToggleEvent { IsOpen = true });
            }
        }

        private void CloseSkillTree()
        {
            if (_skillTreeUI != null)
            {
                _skillTreeUI.gameObject.SetActive(false);
                Time.timeScale = 1f;

                _eventBus.Publish(new SkillTreeToggleEvent { IsOpen = false });
            }
        }

        private void RefreshSkillButtons()
        {
            foreach (var skillButton in _skillButtons)
            {
                if (skillButton != null)
                {
                    skillButton.RefreshState();
                }
            }
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        #endregion

        #region Debug Methods

        private void HandleDebugInput()
        {
            // Debug input for testing
            if (Input.GetKeyDown(KeyCode.F1))
            {
                // Add XP for testing
                _playerSystemImpl.DebugAddXP(50);
                Debug.Log("Debug: Added 50 XP");
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Take damage for testing
                _playerSystemImpl.DebugTakeDamage(25f);
                Debug.Log("Debug: Took 25 damage");
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                // Force next wave
                _enemySystemImpl.ForceNextWave();
                Debug.Log("Debug: Forced next wave");
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                // Print player stats
                _playerSystemImpl.PrintDetailedStats();
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                // Print skill system info
                _skillSystemImpl.PrintSkillSystemInfo();
            }
        }

        #endregion

        #region Public Methods for External Access

        /// <summary>
        /// Get reference to player system (for advanced integrations)
        /// </summary>
        public IPlayerSystem GetPlayerSystem()
        {
            return _playerSystem;
        }

        /// <summary>
        /// Get reference to skill system (for advanced integrations)
        /// </summary>
        public ISkillSystem GetSkillSystem()
        {
            return _skillSystem;
        }

        /// <summary>
        /// Get reference to enemy system (for advanced integrations)
        /// </summary>
        public IEnemySystem GetEnemySystem()
        {
            return _enemySystem;
        }

        #endregion
    }
}