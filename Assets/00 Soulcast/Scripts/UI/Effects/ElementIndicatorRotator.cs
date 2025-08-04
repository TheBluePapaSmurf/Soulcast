// Replace ElementIndicatorRotator.cs with this optimized version
using UnityEngine;

public class ElementIndicatorRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 30f;
    public Vector3 rotationAxis = Vector3.up;
    public bool randomizeStartRotation = true;

    [Header("Performance Settings")]
    public bool enableFrustumCulling = true;
    public float updateRate = 60f; // FPS for rotation updates

    private Camera mainCamera;
    private float lastUpdateTime = 0f;
    private bool isVisible = true;

    void Start()
    {
        mainCamera = Camera.main;

        if (randomizeStartRotation)
        {
            transform.rotation = Random.rotation;
        }
    }

    void Update()
    {
        // Limit update frequency
        float deltaTime = Time.time - lastUpdateTime;
        if (deltaTime < 1f / updateRate) return;
        lastUpdateTime = Time.time;

        // Frustum culling check
        if (enableFrustumCulling && !IsVisible()) return;

        // Rotate using accumulated time for smooth movement
        float rotationAmount = rotationSpeed * deltaTime;
        transform.Rotate(rotationAxis, rotationAmount, Space.Self);
    }

    private bool IsVisible()
    {
        if (mainCamera == null) return true;

        // Simple distance check first (cheaper than frustum)
        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
        if (distance > 50f) return false; // Don't rotate very distant objects

        // Frustum culling
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }
}
