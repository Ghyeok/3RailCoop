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
    [Tooltip("ConstantSpeed 모드에서 전진 속도를 가져올 대상. 플레이어와 같은 속도를 쓴다.")]
    [SerializeField] private CoopController forwardSource;

    private void Start()
    {
        if (movementMode == MovementMode.ConstantSpeed && forwardSource == null)
            Debug.LogError("ConstantSpeed 모드에는 전진 속도를 가져올 CoopController가 필요합니다.", this);
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, roll);

        if (movementMode == MovementMode.FollowTarget)
        {
            if (target != null)
                transform.position = target.position + offset;
        }
        else if (forwardSource != null && forwardSource.isActiveAndEnabled)
        {
            transform.position += Vector3.forward * (forwardSource.ForwardSpeed * Time.deltaTime);
        }
    }
}
