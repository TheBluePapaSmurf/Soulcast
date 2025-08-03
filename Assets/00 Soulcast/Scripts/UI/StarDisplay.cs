using UnityEngine;
using UnityEngine.UI;

public class StarDisplay : MonoBehaviour
{
    [Header("Star Display Settings")]
    public StarConfig[] stars; // Array of star configs (auto-created)

    [Header("Layout")]
    public bool autoCreateStars = true;
    public GameObject starPrefab;
    public int maxStars = 6;

    private bool isInitialized = false;

    void Start()
    {
        InitializeStars();
    }

    void InitializeStars()
    {
        if (isInitialized) return; // Prevent double initialization

        if (autoCreateStars && (stars == null || stars.Length == 0))
        {
            CreateStarImages();
        }

        isInitialized = true;
    }

    // ✅ REPLACE CreateStarImages methode in StarDisplay.cs
    void CreateStarImages()
    {
        if (starPrefab == null)
        {
            Debug.LogError("StarPrefab is null! Cannot create stars.");
            return;
        }

        // Check if the prefab has StarConfig component
        StarConfig prefabConfig = starPrefab.GetComponent<StarConfig>();
        if (prefabConfig == null)
        {
            Debug.LogError("StarPrefab must have StarConfig component!");
            return;
        }

        stars = new StarConfig[maxStars];

        for (int i = 0; i < maxStars; i++)
        {
            GameObject starObj;

            // ✅ FIX: Check if parent is persistent and handle appropriately
            if (IsParentPersistent())
            {
                // Create without parent first, then set parent
                starObj = Instantiate(starPrefab);
                starObj.transform.SetParent(transform, false);
                Debug.Log($"Created star {i} with safe parenting (persistent parent detected)");
            }
            else
            {
                // Normal instantiation
                starObj = Instantiate(starPrefab, transform);
            }

            stars[i] = starObj.GetComponent<StarConfig>();

            if (stars[i] == null)
            {
                Debug.LogError($"Star {i} doesn't have StarConfig component!");
            }
            else
            {
                Debug.Log($"Created star {i} successfully");
                // Initialize as empty star
                stars[i].SetFilled(false);
            }
        }
    }

    // ✅ NEW: Helper method to check if parent hierarchy contains persistent objects
    private bool IsParentPersistent()
    {
        Transform current = transform;
        while (current != null)
        {
            // Check if this GameObject is marked as DontDestroyOnLoad
            if (current.gameObject.scene.name == "DontDestroyOnLoad")
            {
                return true;
            }

            // Check if this has PlayerInventory component (which uses DontDestroyOnLoad)
            if (current.GetComponent<PlayerInventory>() != null)
            {
                return true;
            }

            current = current.parent;
        }
        return false;
    }


    public void SetStarLevel(int currentStars, int maxDisplayStars = 6)
    {
        // IMPORTANT: Initialize stars if not already done
        if (!isInitialized)
        {
            InitializeStars();
        }

        if (stars == null || stars.Length == 0)
        {
            Debug.LogError("Stars array is still null or empty after initialization!");
            Debug.LogError($"starPrefab assigned: {starPrefab != null}");
            Debug.LogError($"autoCreateStars: {autoCreateStars}");
            return;
        }

        // Show/hide and set star states
        for (int i = 0; i < stars.Length && i < maxDisplayStars; i++)
        {
            if (stars[i] != null)
            {
                bool isFilled = i < currentStars;
                stars[i].SetFilled(isFilled);
                stars[i].gameObject.SetActive(true);
            }
        }

        // Hide unused stars
        for (int i = maxDisplayStars; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].gameObject.SetActive(false);
            }
        }
    }

    // Public method to force initialization (useful for debugging)
    public void ForceInitialize()
    {
        isInitialized = false;
        InitializeStars();
    }
}
