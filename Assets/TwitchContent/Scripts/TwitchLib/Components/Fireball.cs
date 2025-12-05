using System.Collections;
using LGD.Gameplay.Polish;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;
    private Coroutine _rotationCoroutine;
    public GameObject _explosionEffectPrefab;
    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    public void Fire(Vector2 direction, float force)
    {
        _rigidbody2D.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        _rotationCoroutine = StartCoroutine(UpdateRotationProcess());
    }

    private IEnumerator UpdateRotationProcess()
    {
        while (true)
        {
            if (_rigidbody2D.linearVelocity.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(_rigidbody2D.linearVelocity.y, _rigidbody2D.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            yield return null;
        }
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        StartCoroutine(HandleCollision(collision));
    }

    public IEnumerator HandleCollision(Collision2D collision)
    {
        if (_rotationCoroutine != null)
        {
            StopCoroutine(_rotationCoroutine);
        }

        if (collision.gameObject.TryGetComponent(out RubberDuckController duck))
            DuckManager.Instance.RemoveDuck(collision.gameObject);

        HandleExplosionForce(collision);
        HandleExplosionEffect(collision);

        yield return new WaitForEndOfFrame();
        Destroy(gameObject);
    }

    public void HandleExplosionForce(Collision2D collision)
    {
        Vector2 explosionPosition = collision.contacts[0].point;
        float explosionRadius = 6f;
        float explosionForce = 13f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(explosionPosition, explosionRadius);
        foreach (Collider2D hit in colliders)
        {
            if (hit.attachedRigidbody != null)
            {
                Vector2 direction = hit.transform.position - new Vector3(explosionPosition.x, explosionPosition.y, hit.transform.position.z);
                direction.Normalize();
                direction.y += Random.Range(0.5f, 2f);
                hit.attachedRigidbody.AddForce(direction * explosionForce, ForceMode2D.Impulse);
            }
        }
    }

    public void HandleExplosionEffect(Collision2D collision)
    {
        GameObject explosionEffect = Instantiate(_explosionEffectPrefab, collision.contacts[0].point, Quaternion.identity, null);
        Destroy(explosionEffect, 2f);
    }
}