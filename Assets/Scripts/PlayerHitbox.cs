using System;
using UnityEngine;

/// <summary>
/// 플레이어가 장애물에 닿으면 알린다. 플레이어는 transform으로 움직이므로 kinematic Rigidbody와 트리거 콜라이더가 필요하다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerHitbox : MonoBehaviour
{
    public event Action HitObstacle;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Obstacle>() != null) HitObstacle?.Invoke();
    }
}
