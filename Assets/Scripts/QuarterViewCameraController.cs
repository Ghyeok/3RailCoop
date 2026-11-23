using UnityEngine;

public class QuarterViewCameraController : MonoBehaviour
{
    private enum MovementMode
    {
        FollowTarget,
        ConstantSpeed
    }

    [Header("Rotation")]
    [SerializeField] private float pitch = 25f;
    [SerializeField] private float yaw;
    [SerializeField] private float roll;

    [Header("Movement")]
    [SerializeField] private MovementMode movementMode = MovementMode.FollowTarget;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -6f);
    [SerializeField, Min(0f)] private float forwardSpeed = 5f;

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, roll);

        if (movementMode == MovementMode.FollowTarget)
        {
            if (target != null)
                transform.position = target.position + offset;
        }
        else
        {
            transform.position += Vector3.forward * (forwardSpeed * Time.deltaTime);
        }
    }
}
