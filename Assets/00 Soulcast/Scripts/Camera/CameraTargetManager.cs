using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class CameraTargetPoint
{
    public string name;
    public Transform targetTransform;      // Camera target position/rotation
    public Transform spawnPoint;           // Associated spawn point
    public bool isPlayerTarget;            // Player vs Enemy target
    public int spawnIndex;                 // Which spawn position (1-4)

    public CameraTargetPoint(string name, Transform target, Transform spawn, bool isPlayer, int index)
    {
        this.name = name;
        this.targetTransform = target;
        this.spawnPoint = spawn;
        this.isPlayerTarget = isPlayer;
        this.spawnIndex = index;
    }
}

public class CameraTargetManager : MonoBehaviour
{
    [Header("Camera Targets")]
    public List<CameraTargetPoint> playerCameraTargets = new List<CameraTargetPoint>();
    public List<CameraTargetPoint> enemyCameraTargets = new List<CameraTargetPoint>();

    [Header("Overview Target")]
    public Transform overviewCameraTarget;  // Main overview position

    [Header("Auto Setup")]
    public bool autoFindTargetsOnStart = true;
    public string playerSpawnPrefix = "PlayerSpawn";
    public string enemySpawnPrefix = "EnemySpawn";
    public string cameraTargetName = "CameraTarget";

    [Header("Target Creation Settings")]
    public Vector3 playerCameraOffset = new Vector3(0, 3, -5);
    public Vector3 playerCameraRotation = new Vector3(20, 0, 0);
    public Vector3 enemyCameraOffset = new Vector3(0, 2, -4);
    public Vector3 enemyCameraRotation = new Vector3(15, 180, 0);

    public static CameraTargetManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (autoFindTargetsOnStart)
        {
            AutoSetupCameraTargets();
        }
    }

    // ✅ Auto-setup camera targets from spawn points
    [ContextMenu("Auto Setup Camera Targets")]
    public void AutoSetupCameraTargets()
    {
        playerCameraTargets.Clear();
        enemyCameraTargets.Clear();

        // Find spawn points parent
        GameObject spawnPointsParent = GameObject.Find("Spawn_Points");
        if (spawnPointsParent == null)
        {
            Debug.LogError("Spawn_Points GameObject not found!");
            return;
        }

        // Setup player camera targets
        SetupTargetsForType(spawnPointsParent, playerSpawnPrefix, true, playerCameraTargets, playerCameraOffset, playerCameraRotation);

        // Setup enemy camera targets
        SetupTargetsForType(spawnPointsParent, enemySpawnPrefix, false, enemyCameraTargets, enemyCameraOffset, enemyCameraRotation);

        // Setup overview target if not assigned
        if (overviewCameraTarget == null)
        {
            CreateOverviewTarget();
        }

        Debug.Log($"Camera targets setup complete: {playerCameraTargets.Count} player targets, {enemyCameraTargets.Count} enemy targets");
    }

    // ✅ Setup targets for player or enemy spawn points
    private void SetupTargetsForType(GameObject spawnParent, string prefix, bool isPlayer, List<CameraTargetPoint> targetList, Vector3 offset, Vector3 rotation)
    {
        for (int i = 1; i <= 4; i++)  // Assuming max 4 spawn points per side
        {
            string spawnName = $"{prefix}{i}";
            Transform spawnPoint = spawnParent.transform.Find(spawnName);

            if (spawnPoint != null)
            {
                // Look for existing camera target
                Transform cameraTarget = spawnPoint.Find(cameraTargetName);

                // Create camera target if it doesn't exist
                if (cameraTarget == null)
                {
                    cameraTarget = CreateCameraTargetForSpawn(spawnPoint, offset, rotation);
                }

                // Add to list
                CameraTargetPoint targetPoint = new CameraTargetPoint(
                    spawnName,
                    cameraTarget,
                    spawnPoint,
                    isPlayer,
                    i
                );

                targetList.Add(targetPoint);
                Debug.Log($"Setup camera target for {spawnName}");
            }
        }
    }

    // ✅ Create camera target child for spawn point
    private Transform CreateCameraTargetForSpawn(Transform spawnPoint, Vector3 offset, Vector3 rotation)
    {
        GameObject cameraTargetObj = new GameObject(cameraTargetName);
        cameraTargetObj.transform.SetParent(spawnPoint);

        // Position camera target relative to spawn point
        cameraTargetObj.transform.localPosition = offset;
        cameraTargetObj.transform.localEulerAngles = rotation;

        return cameraTargetObj.transform;
    }

    // ✅ REPLACE de CreateOverviewTarget methode in CameraTargetManager.cs
    private void CreateOverviewTarget()
    {
        // ✅ First try to find existing MainCameraOverview
        GameObject mainCameraOverview = GameObject.Find("MainCameraOverview");
        if (mainCameraOverview != null)
        {
            overviewCameraTarget = mainCameraOverview.transform;
            Debug.Log("Using existing MainCameraOverview as overview target");
            return;
        }

        // ✅ If not found, create new one
        GameObject overviewObj = new GameObject("OverviewCameraTarget");
        overviewObj.transform.SetParent(transform);
        overviewObj.transform.position = new Vector3(0, 10, -8);
        overviewObj.transform.eulerAngles = new Vector3(45, 0, 0);

        overviewCameraTarget = overviewObj.transform;
        Debug.Log("Created new overview camera target");
    }


    // ✅ Get camera target for specific spawn index
    public Transform GetPlayerCameraTarget(int spawnIndex)
    {
        var target = playerCameraTargets.FirstOrDefault(t => t.spawnIndex == spawnIndex);
        return target?.targetTransform;
    }

    public Transform GetEnemyCameraTarget(int spawnIndex)
    {
        var target = enemyCameraTargets.FirstOrDefault(t => t.spawnIndex == spawnIndex);
        return target?.targetTransform;
    }

    // ✅ Get camera target for monster (based on spawn position)
    public Transform GetCameraTargetForMonster(Monster monster)
    {
        if (monster == null) return null;

        // Find which spawn point this monster is closest to
        int spawnIndex = GetSpawnIndexForMonster(monster);

        if (monster.isPlayerControlled)
        {
            return GetPlayerCameraTarget(spawnIndex);
        }
        else
        {
            return GetEnemyCameraTarget(spawnIndex);
        }
    }

    // ✅ Find which spawn index monster is at
    private int GetSpawnIndexForMonster(Monster monster)
    {
        Vector3 monsterPos = monster.transform.position;
        float closestDistance = float.MaxValue;
        int closestIndex = 1;

        // Check player spawns
        if (monster.isPlayerControlled)
        {
            foreach (var target in playerCameraTargets)
            {
                float distance = Vector3.Distance(monsterPos, target.spawnPoint.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = target.spawnIndex;
                }
            }
        }
        else
        {
            // Check enemy spawns
            foreach (var target in enemyCameraTargets)
            {
                float distance = Vector3.Distance(monsterPos, target.spawnPoint.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = target.spawnIndex;
                }
            }
        }

        return closestIndex;
    }

    // ✅ Get all enemy camera targets (for target switching)
    public List<Transform> GetAllEnemyCameraTargets()
    {
        return enemyCameraTargets.Select(t => t.targetTransform).ToList();
    }

    // ✅ Get all player camera targets
    public List<Transform> GetAllPlayerCameraTargets()
    {
        return playerCameraTargets.Select(t => t.targetTransform).ToList();
    }

    // ✅ Get enemy targets ordered by spawn index for smooth switching
    public List<Transform> GetEnemyTargetsForSwitching()
    {
        return enemyCameraTargets
            .Where(t => HasAliveMonsterAtSpawn(t.spawnPoint))
            .OrderBy(t => t.spawnIndex)
            .Select(t => t.targetTransform)
            .ToList();
    }

    // ✅ Check if spawn point has alive monster
    private bool HasAliveMonsterAtSpawn(Transform spawnPoint)
    {
        // Find monsters near this spawn point
        Collider[] colliders = Physics.OverlapSphere(spawnPoint.position, 2f);
        foreach (var collider in colliders)
        {
            Monster monster = collider.GetComponent<Monster>();
            if (monster != null && monster.isAlive)
            {
                return true;
            }
        }
        return false;
    }

    // ✅ Get overview target
    public Transform GetOverviewTarget()
    {
        return overviewCameraTarget;
    }

    // ✅ Debug methods
    [ContextMenu("Log All Targets")]
    public void LogAllTargets()
    {
        Debug.Log("=== PLAYER CAMERA TARGETS ===");
        foreach (var target in playerCameraTargets)
        {
            Debug.Log($"{target.name}: {target.targetTransform.position}");
        }

        Debug.Log("=== ENEMY CAMERA TARGETS ===");
        foreach (var target in enemyCameraTargets)
        {
            Debug.Log($"{target.name}: {target.targetTransform.position}");
        }
    }

    // ✅ Visual debug in scene view
    void OnDrawGizmos()
    {
        // Draw player camera targets in green
        Gizmos.color = Color.green;
        foreach (var target in playerCameraTargets)
        {
            if (target.targetTransform != null)
            {
                Gizmos.DrawWireSphere(target.targetTransform.position, 0.5f);
                Gizmos.DrawRay(target.targetTransform.position, target.targetTransform.forward * 2f);
            }
        }

        // Draw enemy camera targets in red
        Gizmos.color = Color.red;
        foreach (var target in enemyCameraTargets)
        {
            if (target.targetTransform != null)
            {
                Gizmos.DrawWireSphere(target.targetTransform.position, 0.5f);
                Gizmos.DrawRay(target.targetTransform.position, target.targetTransform.forward * 2f);
            }
        }

        // Draw overview target in blue
        if (overviewCameraTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(overviewCameraTarget.position, Vector3.one);
        }
    }
}
