using UnityEngine;

/// <summary>
/// Spawns a rolling obstacle (like a sphere) at a specific location, destroys it after a set time,
/// and continuously respawns it. Useful for creating repeating hazards like rolling boulders.
/// </summary>
public class RollingObstacleSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    [Tooltip("The prefab to spawn (e.g., your sphere obstacle)")]
    public GameObject obstaclePrefab;

    [Tooltip("Spawn position (leave empty to use this GameObject's position)")]
    public Transform spawnPoint;

    [Header("Timing Settings")]
    [Tooltip("How long the obstacle exists before being destroyed (in seconds)")]
    [Range(1f, 30f)]
    public float lifetimeDuration = 10f;

    [Tooltip("Delay before spawning the first obstacle (in seconds)")]
    [Range(0f, 10f)]
    public float initialDelay = 2f;

    [Tooltip("Delay between destroying an obstacle and spawning the next one (in seconds)")]
    [Range(0f, 10f)]
    public float respawnDelay = 3f;

    [Header("Auto Start")]
    [Tooltip("Start spawning automatically when the scene starts")]
    public bool autoStart = true;

    [Header("Physics Settings")]
    [Tooltip("Apply initial force to the spawned object (optional)")]
    public bool applyInitialForce = false;

    [Tooltip("Force direction (will be normalized)")]
    public Vector3 forceDirection = Vector3.forward;

    [Tooltip("Force magnitude")]
    [Range(0f, 100f)]
    public float forceMagnitude = 10f;

    [Header("Debug")]
    [Tooltip("Show spawn point in the editor")]
    public bool showGizmos = true;

    [Tooltip("Log spawn/destroy events to console")]
    public bool debugLog = false;

    // Private state
    private GameObject currentObstacle;
    private float timer = 0f;
    private bool isActive = false;
    private SpawnerState currentState = SpawnerState.WaitingToStart;

    private enum SpawnerState
    {
        WaitingToStart,     // Waiting for initial delay
        ObstacleActive,     // Obstacle is spawned and alive
        WaitingToRespawn    // Obstacle destroyed, waiting to respawn
    }

    void Start()
    {
        // Validate prefab
        if (obstaclePrefab == null)
        {
            Debug.LogError($"RollingObstacleSpawner on {gameObject.name}: Obstacle Prefab is not assigned!", this);
            enabled = false;
            return;
        }

        // Use this GameObject's position as spawn point if not assigned
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        // Start spawning if auto-start is enabled
        if (autoStart)
        {
            StartSpawning();
        }
    }

    void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;

        switch (currentState)
        {
            case SpawnerState.WaitingToStart:
                if (timer >= initialDelay)
                {
                    SpawnObstacle();
                    timer = 0f;
                }
                break;

            case SpawnerState.ObstacleActive:
                if (timer >= lifetimeDuration)
                {
                    DestroyObstacle();
                    timer = 0f;
                }
                break;

            case SpawnerState.WaitingToRespawn:
                if (timer >= respawnDelay)
                {
                    SpawnObstacle();
                    timer = 0f;
                }
                break;
        }
    }

    /// <summary>
    /// Spawns the obstacle at the spawn point
    /// </summary>
    private void SpawnObstacle()
    {
        if (obstaclePrefab == null || spawnPoint == null)
        {
            Debug.LogWarning($"Cannot spawn obstacle - missing prefab or spawn point on {gameObject.name}");
            return;
        }

        // Spawn the obstacle
        currentObstacle = Instantiate(obstaclePrefab, spawnPoint.position, spawnPoint.rotation);
        currentObstacle.name = $"{obstaclePrefab.name}_Instance";

        // Apply initial force if enabled
        if (applyInitialForce && currentObstacle != null)
        {
            Rigidbody rb = currentObstacle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 force = forceDirection.normalized * forceMagnitude;
                rb.AddForce(force, ForceMode.Impulse);

                if (debugLog)
                {
                    Debug.Log($"Applied force {force} to {currentObstacle.name}");
                }
            }
            else if (debugLog)
            {
                Debug.LogWarning($"Cannot apply force - {currentObstacle.name} has no Rigidbody component");
            }
        }

        currentState = SpawnerState.ObstacleActive;

        if (debugLog)
        {
            Debug.Log($"Spawned obstacle: {currentObstacle.name} at {spawnPoint.position}");
        }
    }

    /// <summary>
    /// Destroys the current obstacle
    /// </summary>
    private void DestroyObstacle()
    {
        if (currentObstacle != null)
        {
            if (debugLog)
            {
                Debug.Log($"Destroying obstacle: {currentObstacle.name}");
            }

            Destroy(currentObstacle);
            currentObstacle = null;
        }

        currentState = SpawnerState.WaitingToRespawn;
    }

    /// <summary>
    /// Starts the spawning cycle
    /// </summary>
    public void StartSpawning()
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning($"Cannot start spawning - no prefab assigned on {gameObject.name}");
            return;
        }

        isActive = true;
        timer = 0f;
        currentState = SpawnerState.WaitingToStart;

        if (debugLog)
        {
            Debug.Log($"Started spawning cycle on {gameObject.name}");
        }
    }

    /// <summary>
    /// Stops the spawning cycle and destroys current obstacle
    /// </summary>
    public void StopSpawning()
    {
        isActive = false;

        if (currentObstacle != null)
        {
            Destroy(currentObstacle);
            currentObstacle = null;
        }

        timer = 0f;
        currentState = SpawnerState.WaitingToStart;

        if (debugLog)
        {
            Debug.Log($"Stopped spawning cycle on {gameObject.name}");
        }
    }

    /// <summary>
    /// Immediately spawns an obstacle (resets the cycle)
    /// </summary>
    public void SpawnNow()
    {
        // Destroy current obstacle if it exists
        if (currentObstacle != null)
        {
            Destroy(currentObstacle);
            currentObstacle = null;
        }

        SpawnObstacle();
        timer = 0f;
    }

    /// <summary>
    /// Toggles spawning on/off
    /// </summary>
    public void ToggleSpawning()
    {
        if (isActive)
            StopSpawning();
        else
            StartSpawning();
    }

    // Draw spawn point in editor
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Transform point = spawnPoint != null ? spawnPoint : transform;

        // Draw spawn point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(point.position, 0.5f);
        Gizmos.DrawRay(point.position, point.forward * 2f);

        // Draw force direction if enabled
        if (applyInitialForce)
        {
            Gizmos.color = Color.yellow;
            Vector3 forceDir = forceDirection.normalized * (forceMagnitude * 0.1f);
            Gizmos.DrawRay(point.position, forceDir);
        }
    }

    // Clean up on disable
    private void OnDisable()
    {
        if (currentObstacle != null)
        {
            Destroy(currentObstacle);
            currentObstacle = null;
        }
    }
}
