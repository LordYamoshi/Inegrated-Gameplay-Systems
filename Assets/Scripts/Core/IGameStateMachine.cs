using UnityEngine;
using System;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Manages game state transitions and updates
    /// </summary>
    public interface IGameStateMachine
    {
        void ChangeState(GameStateType stateType);
        void Update();
        GameStateType CurrentState { get; }
    }
    
    /// <summary>
    /// Game state enumeration
    /// </summary>
    public enum GameStateType
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    /// <summary>
    /// Skill effect types for categorization
    /// </summary>
    public enum SkillEffectType
    {
        Health,
        Speed,
        Damage,
        Defense,
        Utility,
        AttackSpeed,
        Range
    }

    /// <summary>
    /// Game input data structure for player actions
    /// </summary>
    public struct GameInput
    {
        public Vector3 MovementInput;
        public bool SkillTreeTogglePressed;
        public bool RestartPressed;
        public bool PausePressed;
        public float DeltaTime;
    }

    /// <summary>
    /// Combat action data structure for player attacks
    /// </summary>
    public struct CombatAction
    {
        public Vector3 SourcePosition;
        public Vector3 TargetPosition;
        public float Damage;
        public float Range;
        public string ActionType;
    }
}