using UnityEngine;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Represents an enemy in the game with basic AI and combat functionality
    /// </summary>
    public class Enemy
    {
        // Enemy stats
        private float _maxHealth = 50f;
        private float _currentHealth;
        private float _damage = 20f;
        private float _speed = 2f;
        private float _attackRange = 1.5f;
        private float _attackRate = 1f;
        private int _xpReward = 10;
        
        // AI behavior
        private float _detectionRange = 15f;
        private float _attackTimer;
        private bool _isDead = false;
        
        // State
        private EnemyState _currentState = EnemyState.Idle;
        
        // References (managed by GameManager)
        public GameObject GameObject { get; set; }
        public Transform Transform { get; set; }
        public Transform PlayerTarget { get; set; }
        public IEventBus EventBus { get; set; }
        
        // Properties
        public float Health => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float Damage => _damage;
        public bool IsDead => _isDead;
        public EnemyState CurrentState => _currentState;
        public Vector3 Position => Transform?.position ?? Vector3.zero;
        
        /// <summary>
        /// Initialize enemy with custom stats
        /// </summary>
        public void Initialize(float health, float damage, GameObject gameObject, Transform playerTarget, IEventBus eventBus)
        {
            _maxHealth = health;
            _currentHealth = health;
            _damage = damage;
            GameObject = gameObject;
            Transform = gameObject.transform;
            PlayerTarget = playerTarget;
            EventBus = eventBus;
            
            Debug.Log($"Enemy initialized with {health} HP and {damage} damage");
        }
        
        /// <summary>
        /// Update enemy AI and behavior - called by GameManager
        /// </summary>
        public void Update()
        {
            if (_isDead || Transform == null || PlayerTarget == null) return;
            
            UpdateAI();
            UpdateAttackTimer();
        }
        
        /// <summary>
        /// Enemy takes damage
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (_isDead) return;
            
            _currentHealth -= damage;
            
            Debug.Log($"Enemy took {damage} damage. Health: {_currentHealth}/{_maxHealth}");
            
            if (_currentHealth <= 0)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Kill the enemy
        /// </summary>
        public void Die()
        {
            if (_isDead) return;
            
            _isDead = true;
            
            // Publish enemy defeated event
            EventBus?.Publish(new EnemyDefeatedEvent
            {
                Position = Position,
                XPReward = _xpReward,
                EnemyType = "Basic"
            });
            
            Debug.Log($"Enemy died at {Position}, rewarding {_xpReward} XP");
            
            // Request destruction through GameManager (since we can't call Destroy directly)
            RequestDestruction();
        }
        
        /// <summary>
        /// Get movement vector for GameManager to apply
        /// </summary>
        public Vector3 GetMovementVector()
        {
            if (_isDead || Transform == null || PlayerTarget == null) return Vector3.zero;
            
            if (_currentState == EnemyState.Chasing)
            {
                Vector3 direction = (PlayerTarget.position - Transform.position).normalized;
                direction.y = 0; // Keep movement on horizontal plane
                return direction * _speed;
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Check if enemy should attack player
        /// </summary>
        public bool ShouldAttackPlayer()
        {
            if (_isDead || PlayerTarget == null) return false;
            
            return _currentState == EnemyState.Attacking && _attackTimer >= (1f / _attackRate);
        }
        
        /// <summary>
        /// Perform attack - called by GameManager when ShouldAttackPlayer returns true
        /// </summary>
        public void PerformAttack()
        {
            if (_isDead) return;
            
            _attackTimer = 0f;
            
            // Publish attack event for GameManager to handle
            EventBus?.Publish(new PlayerDamagedEvent
            {
                Damage = _damage,
                DamageSource = Position,
                DamageType = "Enemy Attack"
            });
            
            Debug.Log($"Enemy attacks player for {_damage} damage!");
        }
        
        #region Private Methods
        
        private void UpdateAI()
        {
            float distanceToPlayer = Vector3.Distance(Transform.position, PlayerTarget.position);
            
            // State machine for enemy AI
            switch (_currentState)
            {
                case EnemyState.Idle:
                    if (distanceToPlayer <= _detectionRange)
                    {
                        _currentState = EnemyState.Chasing;
                    }
                    break;
                    
                case EnemyState.Chasing:
                    if (distanceToPlayer > _detectionRange * 1.5f) // Hysteresis
                    {
                        _currentState = EnemyState.Idle;
                    }
                    else if (distanceToPlayer <= _attackRange)
                    {
                        _currentState = EnemyState.Attacking;
                    }
                    break;
                    
                case EnemyState.Attacking:
                    if (distanceToPlayer > _attackRange)
                    {
                        _currentState = EnemyState.Chasing;
                    }
                    break;
            }
        }
        
        private void UpdateAttackTimer()
        {
            _attackTimer += Time.deltaTime;
        }
        
        private void RequestDestruction()
        {
            // Since we can't destroy the GameObject directly, we mark for destruction
            // GameManager will handle the actual destruction
            if (GameObject != null)
            {
                GameObject.SetActive(false);
            }
        }
        
        #endregion
        
        #region Debug Methods
        
        /// <summary>
        /// Get debug information about this enemy
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Enemy - Health: {_currentHealth:F1}/{_maxHealth:F1}, State: {_currentState}, Dead: {_isDead}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Enemy AI states
    /// </summary>
    public enum EnemyState
    {
        Idle,
        Chasing,
        Attacking,
        Dead
    }
}