using UnityEngine;
using UnityEngine.InputSystem;

public class AOSCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 150, -120); 
    public float smoothSpeed = 8f;
    public bool isLocked = true;

    void Update()
    {
        // Toggle camera lock with Y key
        if (Keyboard.current.yKey.wasPressedThisFrame)
        {
            isLocked = !isLocked;
            Debug.Log("[AOSCameraFollow] Camera Locked: " + isLocked);
        }

        // Space bar held down temporarily locks to character (LoL behavior)
        if (Keyboard.current.spaceKey.isPressed)
        {
            isLocked = true;
        }
    }

    void LateUpdate()
    {
        if (target == null || !isLocked) return;

        // LoL camera strictly follows the player but with a fixed tilt
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Fixed rotation (approx 55-60 degrees tilt)
        transform.rotation = Quaternion.Euler(55f, 0, 0);
    }
}
