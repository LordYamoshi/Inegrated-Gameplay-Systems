using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// A simple event bus implementation for decoupled event handling
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _eventHandlers = new();
        private readonly Queue<object> _eventQueue = new();
        private bool _isProcessingEvents = false;

        public void Publish<T>(T eventData) where T : class
        {
            if (eventData == null) return;

            // Queue events during processing to avoid re-entrancy issues
            if (_isProcessingEvents)
            {
                _eventQueue.Enqueue(eventData);
                return;
            }

            PublishImmediate(eventData);
            
            // Process any queued events
            ProcessQueuedEvents();
        }

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null) return;

            var eventType = typeof(T);
            if (!_eventHandlers.ContainsKey(eventType))
                _eventHandlers[eventType] = new List<Delegate>();
            
            _eventHandlers[eventType].Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null) return;

            var eventType = typeof(T);
            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType].Remove(handler);
                
                // Clean up empty lists
                if (_eventHandlers[eventType].Count == 0)
                {
                    _eventHandlers.Remove(eventType);
                }
            }
        }

        public void UnsubscribeAll()
        {
            _eventHandlers.Clear();
            _eventQueue.Clear();
        }

        private void PublishImmediate<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            if (!_eventHandlers.ContainsKey(eventType)) return;

            _isProcessingEvents = true;
            
            try
            {
                // Create a copy to avoid modification during iteration
                var handlers = _eventHandlers[eventType].ToList();
                foreach (var handler in handlers)
                {
                    try
                    {
                        ((Action<T>)handler)?.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in event handler for {eventType.Name}: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
            finally
            {
                _isProcessingEvents = false;
            }
        }

        private void ProcessQueuedEvents()
        {
            while (_eventQueue.Count > 0)
            {
                var queuedEvent = _eventQueue.Dequeue();
                var eventType = queuedEvent.GetType();
                
                // Use reflection to call PublishImmediate with the correct type
                var method = typeof(EventBus).GetMethod(nameof(PublishImmediate), 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var genericMethod = method?.MakeGenericMethod(eventType);
                genericMethod?.Invoke(this, new[] { queuedEvent });
            }
        }

        public int GetSubscriberCount<T>() where T : class
        {
            var eventType = typeof(T);
            return _eventHandlers.ContainsKey(eventType) ? _eventHandlers[eventType].Count : 0;
        }

        public int GetTotalSubscriberCount()
        {
            return _eventHandlers.Values.Sum(handlers => handlers.Count);
        }
    }

    #region Core Game Events

    /// <summary>
    /// Published when player health changes
    /// </summary>
    public class PlayerHealthChangedEvent
    {
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
        public float PreviousHealth { get; set; }
        public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0;
        public bool IsHealing => CurrentHealth > PreviousHealth;
        public bool IsCriticalHealth => HealthPercentage <= 0.25f;
    }

    /// <summary>
    /// Published when player XP changes
    /// </summary>
    public class PlayerXPChangedEvent
    {
        public int CurrentXP { get; set; }
        public int XPGained { get; set; }
        public int PreviousXP { get; set; }
        public string Source { get; set; } = "Unknown";
    }

    /// <summary>
    /// Published when an enemy is defeated
    /// </summary>
    public class EnemyDefeatedEvent
    {
        public Vector3 Position { get; set; }
        public int XPReward { get; set; }
        public string EnemyType { get; set; }
        public int WaveNumber { get; set; }
        public float SurvivalTime { get; set; }
    }

    /// <summary>
    /// Published when wave changes
    /// </summary>
    public class WaveChangedEvent
    {
        public int WaveNumber { get; set; }
        public int EnemiesInWave { get; set; }
        public float WaveDifficulty { get; set; }
        public float DelayBeforeStart { get; set; }
    }

    /// <summary>
    /// Published when all enemies in a wave are defeated
    /// </summary>
    public class WaveCompletedEvent
    {
        public int CompletedWave { get; set; }
        public int NextWaveNumber { get; set; }
        public float DelayBeforeNextWave { get; set; }
        public int TotalXPEarned { get; set; }
        public float WaveCompletionTime { get; set; }
    }

    /// <summary>
    /// Published when game ends (victory or defeat)
    /// </summary>
    public class GameOverEvent
    {
        public bool Victory { get; set; }
        public int FinalWave { get; set; }
        public int TotalXP { get; set; }
        public int SkillsUnlocked { get; set; }
        public string Reason { get; set; }
        public float TotalSurvivalTime { get; set; }
        public Dictionary<string, int> Statistics { get; set; } = new();
    }

    /// <summary>
    /// Published when a skill is unlocked
    /// </summary>
    public class SkillUnlockedEvent
    {
        public SkillDefinition Skill { get; set; }
        public int XPSpent { get; set; }
        public int RemainingXP { get; set; }
        public int TotalSkillsUnlocked { get; set; }
    }

    /// <summary>
    /// Published when skill tree is opened/closed
    /// </summary>
    public class SkillTreeToggleEvent
    {
        public bool IsOpen { get; set; }
        public int AvailableSkills { get; set; }
        public int PlayerXP { get; set; }
    }

    /// <summary>
    /// Published when game state changes
    /// </summary>
    public class GameStateChangedEvent
    {
        public GameStateType PreviousState { get; set; }
        public GameStateType NewState { get; set; }
        public float StateTime { get; set; }
        public Dictionary<string, object> StateData { get; set; } = new();
    }

    /// <summary>
    /// Published when player takes damage
    /// </summary>
    public class PlayerDamagedEvent
    {
        public float Damage { get; set; }
        public float ActualDamage { get; set; }
        public Vector3 DamageSource { get; set; }
        public string DamageType { get; set; }
        public bool IsCriticalHit { get; set; }
        public float DamageReduction { get; set; }
    }

    /// <summary>
    /// Published when player attacks
    /// </summary>
    public class PlayerAttackEvent
    {
        public Vector3 AttackPosition { get; set; }
        public float Damage { get; set; }
        public float Range { get; set; }
        public string AttackType { get; set; }
        public bool HitEnemy { get; set; }
        public int EnemiesHit { get; set; }
    }

    #endregion

    #region System Communication Events

    /// <summary>
    /// Request to create an enemy - handled by GameManager
    /// </summary>
    public class CreateEnemyEvent
    {
        public Vector3 Position { get; set; }
        public float Health { get; set; }
        public float Damage { get; set; }
        public float Speed { get; set; }
        public string EnemyType { get; set; }
        public int WaveNumber { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new();
    }

    /// <summary>
    /// Published when enemy spawns successfully
    /// </summary>
    public class EnemySpawnedEvent
    {
        public Vector3 SpawnPosition { get; set; }
        public string EnemyType { get; set; }
        public int WaveNumber { get; set; }
        public int EnemyId { get; set; }
        public float Health { get; set; }
        public float Damage { get; set; }
    }

    /// <summary>
    /// Request to update UI - handled by GameManager
    /// </summary>
    public class UpdateUIEvent
    {
        public string UIElement { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Request for visual effect - handled by GameManager
    /// </summary>
    public class VisualEffectEvent
    {
        public string EffectType { get; set; }
        public Vector3 Position { get; set; }
        public float Duration { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Request for audio effect - handled by GameManager
    /// </summary>
    public class AudioEffectEvent
    {
        public string SoundName { get; set; }
        public Vector3 Position { get; set; }
        public float Volume { get; set; } = 1.0f;
        public float Pitch { get; set; } = 1.0f;
        public bool Is3D { get; set; } = false;
    }

    #endregion

    #region Debug Events

    /// <summary>
    /// Debug event for testing and development
    /// </summary>
    public class DebugEvent
    {
        public string Message { get; set; }
        public string Category { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Performance monitoring event
    /// </summary>
    public class PerformanceEvent
    {
        public string SystemName { get; set; }
        public float UpdateTime { get; set; }
        public int ObjectCount { get; set; }
        public float MemoryUsage { get; set; }
    }

    #endregion
}