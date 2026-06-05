using UnityEngine;

public class LPC_CollisionLogger : MonoBehaviour
{
    private void Start()
    {
        Debug.Log($"[CollisionLogger] Active and monitoring physics events on: {gameObject.name}");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.LogWarning($"[CollisionLogger] COLLISION ENTER: {gameObject.name} hit {collision.gameObject.name}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.LogWarning($"[CollisionLogger] TRIGGER ENTER: {gameObject.name} overlapped {other.gameObject.name}");
    }
}
