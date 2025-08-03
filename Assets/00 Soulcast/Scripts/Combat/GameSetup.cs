using UnityEngine;

public class GameSetup : MonoBehaviour
{
    [Header("Monster Prefabs")]
    public GameObject monsterPrefab; // Base monster prefab with Monster component

    [Header("Monster Data")]
    public MonsterData[] playerMonsterData;
    public MonsterData[] enemyMonsterData;

    [Header("Spawn Positions")]
    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;

    [Header("3D Model Settings")]
    public bool spawnModels = true;
    public Vector3 modelOffset = Vector3.zero; // Offset for model positioning
    public Vector3 modelScale = Vector3.one; // Scale multiplier for models

    [Header("Health Bar Settings")]
    public GameObject healthBarPrefab; // Add this field!
    public bool spawnHealthBars = true;

    void Start()
    {
        SetupGame();
    }

    void SetupGame()
    {
        Debug.Log("GameSetup: Starting game setup with 3D model spawning...");

        // Spawn player monsters
        for (int i = 0; i < Mathf.Min(playerSpawnPoints.Length, playerMonsterData.Length); i++)
        {
            if (i < playerSpawnPoints.Length)
            {
                SpawnMonster(
                    playerMonsterData[i],
                    playerSpawnPoints[i],
                    true,
                    "(Player)"
                );
            }
        }

        // Spawn enemy monsters
        for (int i = 0; i < Mathf.Min(6, enemyMonsterData.Length); i++)
        {
            if (i < enemySpawnPoints.Length)
            {
                SpawnMonster(
                    enemyMonsterData[i],
                    enemySpawnPoints[i],
                    false,
                    "(Enemy)"
                );
            }
        }

        Debug.Log("GameSetup: Game setup complete!");
    }

    private void SpawnMonster(MonsterData monsterData, Transform spawnPoint, bool isPlayerControlled, string suffix)
    {
        // 1. Create the base monster GameObject
        GameObject monster = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);

        // 2. Setup Monster component
        Monster monsterComponent = monster.GetComponent<Monster>();
        monsterComponent.monsterData = monsterData;
        monsterComponent.isPlayerControlled = isPlayerControlled;

        // 3. Spawn the 3D model if available
        if (spawnModels && monsterData.modelPrefab != null)
        {
            GameObject model3D = SpawnModel(monsterData.modelPrefab, monster.transform);
            Debug.Log($"Spawned 3D model for {monsterData.monsterName}: {model3D.name}");
        }
        else if (spawnModels && monsterData.modelPrefab == null)
        {
            Debug.LogWarning($"No 3D model prefab assigned for {monsterData.monsterName}!");
            CreateFallbackVisual(monster.transform, monsterData);
        }

        // 4. Initialize the monster
        monsterComponent.InitializeMonster();

        // 5. Set the name
        monster.name = monsterData.monsterName + " " + suffix;

        Debug.Log($"Spawned {monster.name} at {spawnPoint.position}");
    }

    private GameObject SpawnModel(GameObject modelPrefab, Transform parent)
    {
        // Instantiate the 3D model as a child of the monster
        GameObject model = Instantiate(modelPrefab, parent);

        // Apply position offset
        model.transform.localPosition = modelOffset;

        // Apply scale
        model.transform.localScale = Vector3.Scale(model.transform.localScale, modelScale);

        // Ensure the model is properly oriented
        model.transform.localRotation = Quaternion.identity;

        // Name the model
        model.name = "3D_Model";

        // Add ModelController component for visual effects
        ModelController modelController = model.GetComponent<ModelController>();
        if (modelController == null)
        {
            modelController = model.AddComponent<ModelController>();
        }

        // Set health bar prefab reference
        if (spawnHealthBars && healthBarPrefab != null)
        {
            modelController.healthBarPrefab = healthBarPrefab;
        }

        return model;
    }

    private void CreateFallbackVisual(Transform parent, MonsterData monsterData)
    {
        // Create a simple primitive as fallback
        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fallback.transform.SetParent(parent);
        fallback.transform.localPosition = modelOffset;
        fallback.transform.localScale = modelScale;
        fallback.name = "Fallback_Visual";

        // Color it based on element
        Renderer renderer = fallback.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = ElementalSystem.GetElementColor(monsterData.element);
        }

        // Add ModelController even for fallback visuals
        ModelController modelController = fallback.AddComponent<ModelController>();
        if (spawnHealthBars && healthBarPrefab != null)
        {
            modelController.healthBarPrefab = healthBarPrefab;
        }

        Debug.Log($"Created fallback visual for {monsterData.monsterName}");
    }

    // Debug method to respawn all monsters
    [ContextMenu("Respawn All Monsters")]
    [System.Obsolete]
    public void RespawnMonsters()
    {
        // Clear existing monsters
        Monster[] existingMonsters = FindObjectsOfType<Monster>();
        for (int i = 0; i < existingMonsters.Length; i++)
        {
            if (Application.isPlaying)
                Destroy(existingMonsters[i].gameObject);
            else
                DestroyImmediate(existingMonsters[i].gameObject);
        }

        // Respawn
        SetupGame();
    }
}
