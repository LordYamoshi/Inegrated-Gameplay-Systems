using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Manages enemy spawning, waves, and enemy lifecycle
    /// </summary>
    public class EnemySystem : IEnemySystem
    {
        private readonly IEventBus _eventBus;
        
        // Wave configuration
        private int _currentWave = 1;
        private bool _waveActive = false;
        private float _waveTimer = 0f;
        private float _timeBetweenWaves = 5f;
        private int _enemiesPerWave = 3;
        private float _waveSpawnRadius = 10f;
        private Transform _playerTransform;
        
        // Enemy tracking
        private int _enemiesSpawnedThisWave = 0;
        private int _enemiesKilledThisWave = 0;
        
        // Wave scaling
        private readonly float _healthScaling = 0.2f;
        private readonly float _damageScaling = 0.15f;
        private readonly int _enemyCountScaling = 1;

        public void OnEnemyDefeated()
        {
            // This method is called by the event bus when an enemy is defeated
            _enemiesKilledThisWave++;
            Debug.Log($"Enemy defeated! Enemies remaining in wave: {EnemiesAlive}");
        }

        public int CurrentWave => _currentWave;
        public int EnemiesAlive => _enemiesSpawnedThisWave - _enemiesKilledThisWave;

        public EnemySystem(GameObject enemyPrefab, Transform spawnParent, IEventBus eventBus)
        {
            _eventBus = eventBus;
            
            // Subscribe to enemy defeated events
            _eventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            
            // Find player transform
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }
        }

        public void SpawnWave(int waveNumber)
        {
            if (_waveActive)
            {
                Debug.LogWarning("Cannot spawn wave - wave already active");
                return;
            }
            
            _currentWave = waveNumber;
            _waveActive = true;
            _enemiesSpawnedThisWave = 0;
            _enemiesKilledThisWave = 0;
            
            // Calculate wave parameters
            int enemyCount = _enemiesPerWave + (_currentWave - 1) * _enemyCountScaling;
            float enemyHealth = 50f + (_currentWave - 1) * 50f * _healthScaling;
            float enemyDamage = 20f + (_currentWave - 1) * 20f * _damageScaling;
            
            Debug.Log($"Starting Wave {_currentWave} with {enemyCount} enemies");
            
            // Spawn enemies
            for (int i = 0; i < enemyCount; i++)
            {
                SpawnEnemy(enemyHealth, enemyDamage);
                _enemiesSpawnedThisWave++;
            }
            
            // Publish wave start event
            _eventBus.Publish(new WaveChangedEvent
            {
                WaveNumber = _currentWave,
                EnemiesInWave = enemyCount,
                WaveDifficulty = _currentWave
            });
        }

        public void Update()
        {
            UpdateWaveLogic();
        }

        #region Private Methods

        private void UpdateWaveLogic()
        {
            if (!_waveActive)
            {
                // Wait between waves
                _waveTimer += Time.deltaTime;
                
                if (_waveTimer >= _timeBetweenWaves)
                {
                    _waveTimer = 0f;
                    SpawnWave(_currentWave + 1);
                }
            }
            else
            {
                // Check if wave is complete
                if (EnemiesAlive <= 0)
                {
                    CompleteWave();
                }
            }
        }

        private void SpawnEnemy(float health, float damage)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            // Request enemy creation through event system
            _eventBus.Publish(new CreateEnemyEvent
            {
                Position = spawnPosition,
                Health = health,
                Damage = damage,
                EnemyType = "Basic",
                WaveNumber = _currentWave
            });
            
            // Publish spawn event for tracking
            _eventBus.Publish(new EnemySpawnedEvent
            {
                SpawnPosition = spawnPosition,
                EnemyType = "Basic",
                WaveNumber = _currentWave
            });
            
            Debug.Log($"Requested enemy spawn at {spawnPosition} with {health} HP and {damage} damage");
        }

        private Vector3 GetRandomSpawnPosition()
        {
            if (_playerTransform == null)
            {
                // Fallback to origin if no player found
                return Random.insideUnitSphere * _waveSpawnRadius;
            }
            
            // Spawn enemies in a circle around the player
            Vector2 randomCircle = Random.insideUnitCircle.normalized * _waveSpawnRadius;
            Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            
            return _playerTransform.position + spawnOffset;
        }

        private void CompleteWave()
        {
            _waveActive = false;
            
            Debug.Log($"Wave {_currentWave} completed!");
            
            // Publish wave completed event
            _eventBus.Publish(new WaveCompletedEvent
            {
                CompletedWave = _currentWave,
                NextWaveNumber = _currentWave + 1,
                DelayBeforeNextWave = _timeBetweenWaves
            });
            
            // Check for victory condition
            if (_currentWave >= 10) // Victory after 10 waves
            {
                _eventBus.Publish(new GameOverEvent
                {
                    Victory = true,
                    FinalWave = _currentWave,
                    Reason = "Survived 10 waves!",
                    TotalXP = 0, 
                    SkillsUnlocked = 0 
                });
            }
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            _enemiesKilledThisWave++;
            Debug.Log($"Enemy defeated at {evt.Position}, awarded {evt.XPReward} XP. Enemies remaining: {EnemiesAlive}");
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Force start the next wave
        /// </summary>
        public void ForceNextWave()
        {
            if (_waveActive)
            {
                // Force complete current wave
                _enemiesKilledThisWave = _enemiesSpawnedThisWave;
            }
            else
            {
                _waveTimer = _timeBetweenWaves;
            }
        }

        /// <summary>
        /// Configure wave parameters
        /// </summary>
        public void SetWaveParameters(int enemiesPerWave, float timeBetweenWaves, float spawnRadius)
        {
            _enemiesPerWave = enemiesPerWave;
            _timeBetweenWaves = timeBetweenWaves;
            _waveSpawnRadius = spawnRadius;
        }

        #endregion
    }
}