    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "MapData", menuName = "Game/MapData")]
    public class MapData : ScriptableObject
    {
        [Header("Map Info")]
        [Tooltip("Name of the map as displayed in the UI")]
        public string mapName;
        [Tooltip("Description of the map for lore or gameplay context")]
        public string sceneName;
        public string backgroundMusic;

        [Header("Spawn Settings")]
        public float spawnRadius = 10f;
        public int maxEnemiesAtOnce = 50;
        public float wallCheckRadius = 1f;

        [Header("Wave Settings")]
        public int baseEnemyCount = 10;
        public float enemyCountMultiplier = 1.5f;
        public int maxWaves = 10;

        [Header("Enemy Data")]
        public List<EnemyData> meleeEnemies;
        public List<EnemyData> fastEnemies;
        public List<EnemyData> rangedEnemies;
        public List<EnemyData> bossEnemies;

        [Header("Enemy Prefabs")]
        public GameObject meleeEnemyPrefab;
        public GameObject fastEnemyPrefab;
        public GameObject rangedEnemyPrefab;
        public GameObject bossPrefab;

        [Header("Light Gate Settings")]
        [Tooltip("Prefab for the light gate")]
        public GameObject lightGatePrefab;
        [Tooltip("List of possible spawn positions for the light gate")]
        public List<Vector2> lightGateSpawnPoints;
        [Tooltip("Radius to check for valid spawn position (avoid walls)")]
        public float lightGateSpawnCheckRadius = 2f;
}
