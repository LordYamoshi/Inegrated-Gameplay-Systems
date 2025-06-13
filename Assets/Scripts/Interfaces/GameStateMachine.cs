using UnityEngine;
using System.Collections.Generic;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Interface for the game state machine
    /// </summary>
    public class GameStateMachine : IGameStateMachine
    {
        private readonly IEventBus _eventBus;
        private readonly Dictionary<GameStateType, GameState> _states;
        private GameState _currentState;
        private GameStateType _currentStateType;
        private float _stateTime = 0f;
        private readonly Stack<GameStateType> _stateHistory = new Stack<GameStateType>();

        public GameStateType CurrentState => _currentStateType;
        public float StateTime => _stateTime;
        public bool CanGoBack => _stateHistory.Count > 1;

        public GameStateMachine(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
            _states = new Dictionary<GameStateType, GameState>();
            
            InitializeStates();
            
            // Start with Playing state for prototype
            _currentStateType = GameStateType.Playing;
            _currentState = _states[_currentStateType];
            _stateHistory.Push(_currentStateType);
            
            Debug.Log("GameStateMachine initialized with Playing state");
        }

        public void ChangeState(GameStateType stateType)
        {
            if (_currentStateType == stateType)
            {
                Debug.Log($"Already in state: {stateType}");
                return;
            }

            if (!_states.ContainsKey(stateType))
            {
                Debug.LogError($"State {stateType} not found in state machine!");
                return;
            }

            var previousState = _currentStateType;
            
            // Exit current state
            _currentState?.Exit();
            
            // Change to new state
            _currentStateType = stateType;
            _currentState = _states[stateType];
            _stateTime = 0f;
            
            // Add to history (keep last 10 states)
            _stateHistory.Push(stateType);
            if (_stateHistory.Count > 10)
            {
                var temp = new Stack<GameStateType>();
                for (int i = 0; i < 10; i++)
                {
                    temp.Push(_stateHistory.Pop());
                }
                _stateHistory.Clear();
                while (temp.Count > 0)
                {
                    _stateHistory.Push(temp.Pop());
                }
            }
            
            // Enter new state
            _currentState.Enter();
            
            Debug.Log($"State changed: {previousState} → {stateType}");
            
            // Publish state change event
            _eventBus.Publish(new GameStateChangedEvent
            {
                PreviousState = previousState,
                NewState = stateType,
                StateTime = 0f,
                StateData = new Dictionary<string, object>
                {
                    ["Timestamp"] = Time.time,
                    ["PreviousStateTime"] = _stateTime
                }
            });
        }

        public void Update()
        {
            _stateTime += Time.deltaTime;
            _currentState?.Update();
        }

        /// <summary>
        /// Go back to the previous state
        /// </summary>
        public void GoToPreviousState()
        {
            if (_stateHistory.Count <= 1) return;
            
            _stateHistory.Pop(); // Remove current state
            var previousState = _stateHistory.Peek();
            ChangeState(previousState);
        }

        /// <summary>
        /// Force a specific state without history tracking
        /// </summary>
        public void ForceState(GameStateType stateType)
        {
            _stateHistory.Clear();
            ChangeState(stateType);
        }

        private void InitializeStates()
        {
            _states[GameStateType.MainMenu] = new MainMenuState(_eventBus);
            _states[GameStateType.Playing] = new PlayingState(_eventBus);
            _states[GameStateType.Paused] = new PausedState(_eventBus);
            _states[GameStateType.GameOver] = new GameOverState(_eventBus);
            _states[GameStateType.Victory] = new VictoryState(_eventBus);
        }

        /// <summary>
        /// Get state machine statistics
        /// </summary>
        public StateMachineInfo GetStateMachineInfo()
        {
            return new StateMachineInfo
            {
                CurrentState = _currentStateType,
                StateTime = _stateTime,
                StateHistory = new List<GameStateType>(_stateHistory),
                AvailableStates = new List<GameStateType>(_states.Keys)
            };
        }
    }

    #region Abstract Base State

    /// <summary>
    /// Base class for all game states
    /// </summary>
    public abstract class GameState
    {
        protected IEventBus EventBus;
        protected float StateTime = 0f;
        protected readonly Dictionary<string, object> StateData = new Dictionary<string, object>();

        protected GameState(IEventBus eventBus)
        {
            EventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
        }

        public abstract void Enter();
        public abstract void Exit();
        public abstract void Update();

        /// <summary>
        /// Handle input that's common to all states
        /// </summary>
        protected virtual void HandleInput(GameInput input)
        {
            // Base implementation - can be overridden by states
        }

        /// <summary>
        /// Get state-specific data
        /// </summary>
        public Dictionary<string, object> GetStateData()
        {
            return new Dictionary<string, object>(StateData);
        }
    }

    #endregion

    #region Concrete States

    /// <summary>
    /// Main menu state - initial game state
    /// </summary>
    public class MainMenuState : GameState
    {
        public MainMenuState(IEventBus eventBus) : base(eventBus) { }

        public override void Enter()
        {
            Debug.Log("Entered Main Menu State");
            StateTime = 0f;
            
            // Request UI changes through events
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "MainMenu",
                Data = new Dictionary<string, object>
                {
                    ["Show"] = true,
                    ["TimeScale"] = 0f
                }
            });
        }

        public override void Exit()
        {
            Debug.Log("Exited Main Menu State");
            
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "MainMenu",
                Data = new Dictionary<string, object>
                {
                    ["Show"] = false,
                    ["TimeScale"] = 1f
                }
            });
        }

        public override void Update()
        {
            StateTime += Time.deltaTime;
            
            // Handle menu input
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                // Transition to playing state would be requested through events
                EventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "StateTransition",
                    Data = new Dictionary<string, object>
                    {
                        ["RequestedState"] = GameStateType.Playing
                    }
                });
            }
        }
    }

    /// <summary>
    /// Playing state - main game loop
    /// </summary>
    public class PlayingState : GameState
    {
        private bool _skillTreeOpen = false;

        public PlayingState(IEventBus eventBus) : base(eventBus) { }

        public override void Enter()
        {
            Debug.Log("Entered Playing State");
            StateTime = 0f;
            
            // Subscribe to game events
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<SkillTreeToggleEvent>(OnSkillTreeToggle);
            
            // Request game UI activation
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "GameUI",
                Data = new Dictionary<string, object>
                {
                    ["Show"] = true,
                    ["TimeScale"] = 1f
                }
            });

            StateData["StartTime"] = Time.time;
            StateData["SkillTreeOpenCount"] = 0;
        }

        public override void Exit()
        {
            Debug.Log("Exited Playing State");
            
            // Unsubscribe from events
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<SkillTreeToggleEvent>(OnSkillTreeToggle);
            
            StateData["EndTime"] = Time.time;
            StateData["TotalPlayTime"] = Time.time - (float)StateData.GetValueOrDefault("StartTime", Time.time);
        }

        public override void Update()
        {
            StateTime += Time.deltaTime;
            HandlePlayingInput();
        }

        private void HandlePlayingInput()
        {
            // Handle pause input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_skillTreeOpen)
                {
                    // Close skill tree first
                    EventBus.Publish(new SkillTreeToggleEvent { IsOpen = false });
                }
                else
                {
                    // Request pause
                    EventBus.Publish(new UpdateUIEvent
                    {
                        UIElement = "StateTransition",
                        Data = new Dictionary<string, object>
                        {
                            ["RequestedState"] = GameStateType.Paused
                        }
                    });
                }
            }
            
            // Handle skill tree toggle
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Space))
            {
                bool newState = !_skillTreeOpen;
                EventBus.Publish(new SkillTreeToggleEvent 
                { 
                    IsOpen = newState,
                    AvailableSkills = 0, // Would be filled by skill system
                    PlayerXP = 0 // Would be filled by player system
                });
            }
            
            // Debug inputs
            HandleDebugInput();
        }

        private void HandleDebugInput()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                EventBus.Publish(new DebugEvent
                {
                    Message = "Debug: Add XP requested",
                    Category = "Debug",
                    Data = new Dictionary<string, object> { ["XPAmount"] = 50 }
                });
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                EventBus.Publish(new DebugEvent
                {
                    Message = "Debug: Take damage requested",
                    Category = "Debug",
                    Data = new Dictionary<string, object> { ["Damage"] = 25 }
                });
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                EventBus.Publish(new DebugEvent
                {
                    Message = "Debug: Force next wave requested",
                    Category = "Debug"
                });
            }
        }

        private void OnGameOver(GameOverEvent evt)
        {
            var targetState = evt.Victory ? GameStateType.Victory : GameStateType.GameOver;
            
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "StateTransition",
                Data = new Dictionary<string, object>
                {
                    ["RequestedState"] = targetState,
                    ["GameOverData"] = evt
                }
            });
        }

        private void OnSkillTreeToggle(SkillTreeToggleEvent evt)
        {
            _skillTreeOpen = evt.IsOpen;
            
            if (_skillTreeOpen)
            {
                StateData["SkillTreeOpenCount"] = (int)StateData.GetValueOrDefault("SkillTreeOpenCount", 0) + 1;
            }
            
            // Request time scale change
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "TimeScale",
                Data = new Dictionary<string, object>
                {
                    ["TimeScale"] = evt.IsOpen ? 0f : 1f
                }
            });
        }
    }

    /// <summary>
    /// Paused state - game is paused
    /// </summary>
    public class PausedState : GameState
    {
        public PausedState(IEventBus eventBus) : base(eventBus) { }

        public override void Enter()
        {
            Debug.Log("Entered Paused State");
            StateTime = 0f;
            
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "PauseMenu",
                Data = new Dictionary<string, object>
                {
                    ["Show"] = true,
                    ["TimeScale"] = 0f
                }
            });

            StateData["PauseStartTime"] = Time.unscaledTime;
        }

        public override void Exit()
        {
            Debug.Log("Exited Paused State");
            
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "PauseMenu",
                Data = new Dictionary<string, object>
                {
                    ["Show"] = false,
                    ["TimeScale"] = 1f
                }
            });

            StateData["PauseDuration"] = Time.unscaledTime - (float)StateData.GetValueOrDefault("PauseStartTime", Time.unscaledTime);
        }

        public override void Update()
        {
            StateTime += Time.unscaledDeltaTime; // Use unscaled time since game is paused
            
            // Handle resume input
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            {
                EventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "StateTransition",
                    Data = new Dictionary<string, object>
                    {
                        ["RequestedState"] = GameStateType.Playing
                    }
                });
            }
        }
    }

    /// <summary>
    /// Game over state - player has lost
    /// </summary>
    public class GameOverState : GameState
    {
        private GameOverEvent _gameOverData;

        public GameOverState(IEventBus eventBus) : base(eventBus) { }

        public override void Enter()
        {
            Debug.Log("Entered Game Over State");
            StateTime = 0f;
            
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "GameOverScreen",
                Data = new Dictionary<string, object>
                {
                    ["Show"] = true,
                    ["TimeScale"] = 0f,
                    ["Victory"] = false
                }
            });

            StateData["GameOverTime"] = Time.time;
        }

        public override void Exit()
        {
            Debug.Log("Exited Game Over State");
            
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "GameOverScreen",
                Data = new Dictionary<string, object>
                {
                    ["Show"] = false,
                    ["TimeScale"] = 1f
                }
            });
        }

        public override void Update()
        {
            StateTime += Time.unscaledDeltaTime;
            
            // Handle restart input
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
            {
                EventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "RestartGame",
                    Data = new Dictionary<string, object>
                    {
                        ["RestartRequested"] = true
                    }
                });
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "StateTransition",
                    Data = new Dictionary<string, object>
                    {
                        ["RequestedState"] = GameStateType.MainMenu
                    }
                });
            }
        }

        public void SetGameOverData(GameOverEvent gameOverData)
        {
            _gameOverData = gameOverData;
            StateData["GameOverData"] = gameOverData;
        }
    }

    /// <summary>
    /// Victory state - player has won
    /// </summary>
    public class VictoryState : GameState
    {
        public VictoryState(IEventBus eventBus) : base(eventBus) { }

        public override void Enter()
        {
            Debug.Log("Entered Victory State");
            StateTime = 0f;
            
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "VictoryScreen",
                Data = new Dictionary<string, object>
                {
                    ["Show"] = true,
                    ["TimeScale"] = 0f,
                    ["Victory"] = true
                }
            });

            StateData["VictoryTime"] = Time.time;
            
            // Play victory effects
            EventBus.Publish(new AudioEffectEvent
            {
                SoundName = "Victory",
                Volume = 0.8f
            });
        }

        public override void Exit()
        {
            Debug.Log("Exited Victory State");
            
            EventBus.Publish(new UpdateUIEvent
            {
                UIElement = "VictoryScreen",
                Data = new Dictionary<string, object>
                {
                    ["Show"] = false,
                    ["TimeScale"] = 1f
                }
            });
        }

        public override void Update()
        {
            StateTime += Time.unscaledDeltaTime;
            
            // Handle continue input
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
            {
                EventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "RestartGame",
                    Data = new Dictionary<string, object>
                    {
                        ["RestartRequested"] = true
                    }
                });
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EventBus.Publish(new UpdateUIEvent
                {
                    UIElement = "StateTransition",
                    Data = new Dictionary<string, object>
                    {
                        ["RequestedState"] = GameStateType.MainMenu
                    }
                });
            }
        }
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// Information about the state machine's current state, time, history, and available states
    /// </summary>
    public class StateMachineInfo
    {
        public GameStateType CurrentState { get; set; }
        public float StateTime { get; set; }
        public List<GameStateType> StateHistory { get; set; } = new List<GameStateType>();
        public List<GameStateType> AvailableStates { get; set; } = new List<GameStateType>();
    }

    #endregion
}