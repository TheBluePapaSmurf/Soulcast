using UnityEngine;

public class SlowMotionManager : MonoBehaviour
{
    public static SlowMotionManager Instance;

    [Header("Slow Motion Settings")]
    public float slowMotionScale = 0.3f;
    public float transitionSpeed = 5f;

    private float originalTimeScale = 1f;
    private bool isSlowMotion = false;

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
        isSlowMotion = true;
        Time.timeScale = slowMotionScale;
    }

    public void DeactivateSlowMotion()
    {
        isSlowMotion = false;
        Time.timeScale = originalTimeScale;
    }
}
