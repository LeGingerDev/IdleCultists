using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    public float duration;

    private void Start()
    {
        Destroy(gameObject, duration);
    }
}
