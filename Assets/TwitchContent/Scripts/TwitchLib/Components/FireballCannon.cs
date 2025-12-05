using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class FireballCannon : MonoBehaviour
{
    public Transform _fireballSpawnPoint;
    public Transform _cannonTransform;
    public Fireball _fireballPrefab;
    public float _fireballForce = 15f;
    public float _rotationDuration = 0.2f;
    public float _adjustedRotation;
    private SummonDuckReward _summonDuckReward;
    private Queue<string> fireballQueue = new Queue<string>();
    private Coroutine _launchProcess;


    public void Awake()
    {
        _summonDuckReward = FindFirstObjectByType<SummonDuckReward>();
    }

    public void TriggerLaunchFireball(string sender)
    {
        fireballQueue.Enqueue(sender);

        if (_launchProcess == null)
        {
            _launchProcess = StartCoroutine(LaunchFireballProcess());
        }
    }

    public IEnumerator LaunchFireballProcess()
    {
        while (fireballQueue.Count > 0)
        {
            if (_summonDuckReward.activeDucks.Count == 0)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            GameObject duckToTarget = _summonDuckReward.GetRandomDuck();
            Vector3 targetPosition = duckToTarget.transform.position;

            Quaternion initialRotation = _cannonTransform.rotation;
            Vector3 directionToTarget = targetPosition - _cannonTransform.position;
            float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            targetAngle += _adjustedRotation; // Add adjustment here

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

            float elapsedTime = 0f;
            while (elapsedTime < _rotationDuration)
            {
                _cannonTransform.rotation = Quaternion.Slerp(initialRotation, targetRotation, elapsedTime / _rotationDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            _cannonTransform.rotation = targetRotation;

            LaunchFireball(directionToTarget);
            PlayFireEffect();

            fireballQueue.Dequeue();
        }

        _launchProcess = null;
    }

    public void ImmediateFireCannon()
    {
        if (_summonDuckReward.activeDucks.Count == 0)
        {
            return;
        }

        GameObject duckToTarget = _summonDuckReward.GetRandomDuck();
        Vector3 targetPosition = duckToTarget.transform.position;
        Vector3 directionToTarget = targetPosition - _cannonTransform.position;

        float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
        targetAngle += _adjustedRotation; // Add adjustment here too

        _cannonTransform.rotation = Quaternion.Euler(0f, 0f, targetAngle);

        LaunchFireball(directionToTarget);
        PlayFireEffect();
    }

    private void PlayFireEffect()
    {
        _cannonTransform.DOPunchScale(Vector3.one * 0.4f, 0.2f, 10, 1);
    }

    public void LaunchFireball(Vector3 direction)
    {
        Fireball fireball = Instantiate(_fireballPrefab, _fireballSpawnPoint.position, Quaternion.identity);
        fireball.Fire(direction, _fireballForce);
    }
}