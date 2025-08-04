using UnityEngine;
using UnityEditor;

public class HealthBarPrefabCreator : MonoBehaviour
{
    [ContextMenu("Create Health Bar Prefab")]
    void CreateHealthBarPrefab()
    {
        GameObject prefab = HealthBarFactory.CreateFloatingHealthBarPrefab();

        // Save as prefab
        string prefabPath = "Assets/Prefabs/FloatingHealthBar.prefab";

        // Create Prefabs folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        DestroyImmediate(prefab);

        Debug.Log($"Health bar prefab created at: {prefabPath}");
    }
}
