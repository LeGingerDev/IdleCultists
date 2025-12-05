using UnityEngine;
using Sirenix.OdinInspector;

public class RandomizeAnimationStart : MonoBehaviour
{
    [FoldoutGroup("Settings")]
    [SerializeField] private string _animationStateName = "YourAnimationName";
    
    [FoldoutGroup("Settings")]
    [SerializeField] private float _maxRandomStartTime = 7f;
    
    [FoldoutGroup("Settings")]
    [SerializeField] private int _animatorLayer = 0;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        RandomizeStartTime();
    }

    private void RandomizeStartTime()
    {
        if (_animator == null)
        {
            Debug.LogError("No Animator component found!");
            return;
        }

        // Calculate a random normalized time (0 to 1) for the first 7 seconds of a 10 second animation
        // normalized time = actual time / animation length
        float randomTime = Random.Range(0f, _maxRandomStartTime);
        
        // Get the current state info to find the animation length
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(_animatorLayer);
        float animationLength = stateInfo.length;
        
        // Convert to normalized time (0-1 range)
        float normalizedTime = randomTime / animationLength;
        
        // Play the animation from that point
        _animator.Play(_animationStateName, _animatorLayer, normalizedTime);
    }
}