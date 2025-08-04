using UnityEngine;

public class RotatingObject : MonoBehaviour
{
    public Vector3 rotationSpeed = Vector3.up * 30f;

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
