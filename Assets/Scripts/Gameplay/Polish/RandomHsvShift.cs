using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RandomHsvShift : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private Material _material;

    public float lastAssignedValue = 0f;

    private void Start()
    {
        InitializeMaterial();
        EnableHsvShift();
        RandomizeHsvValue();
    }

    private void InitializeMaterial()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        _material = _spriteRenderer.material;
    }

    private void EnableHsvShift()
    {
        _material.EnableKeyword("HSV_ON");
    }

    private void RandomizeHsvValue()
    {
        float randomShift = Random.Range(-180f, 180f);
        lastAssignedValue = randomShift;
        _material.SetFloat("_HsvShift", randomShift);
    }

    public void SetHSVValue(float hsvValue)
    {
        if (_material == null)
            InitializeMaterial();

        lastAssignedValue = hsvValue;
        _material.SetFloat("_HsvShift", hsvValue);
    }

    private void OnValidate()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public float GetAssignedValue()
    {

        return lastAssignedValue;
    }
}