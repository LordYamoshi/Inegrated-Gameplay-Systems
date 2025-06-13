using UnityEngine;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Interface for managing enemy spawning, AI, and combat
    /// </summary>
    public interface IEnemySystem
    {
        void SpawnWave(int waveNumber);
        void Update();
        void OnEnemyDefeated();
        int CurrentWave { get; }
        int EnemiesAlive { get; }
        void ForceNextWave();
        void SetWaveParameters(int enemiesPerWave, float timeBetweenWaves, float spawnRadius);
    }   
}