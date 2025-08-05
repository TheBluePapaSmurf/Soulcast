using UnityEngine;

public class SlowMotionManager : MonoBehaviour
{
    public static SlowMotionManager Instance;

    [Header("Slow Motion Settings")]
    public float slowMotionScale = 0.3f;
    public float transitionSpeed = 5f;

    private float originalTimeScale = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("SlowMotionManager Instance set successfully");
        }
        else
        {
            Debug.LogWarning("Multiple SlowMotionManager instances found - destroying duplicate");
            Destroy(gameObject);
        }
    }

    public void ActivateSlowMotion()
    {
        Time.timeScale = slowMotionScale;
    }

    public void DeactivateSlowMotion()
    {
        Time.timeScale = originalTimeScale;
    }
}
