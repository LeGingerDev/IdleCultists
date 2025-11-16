using UnityEngine;
using UnityEngine.Rendering.Universal;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(Light2D))]
public class Light2DCircleGenerator : MonoBehaviour
{
    [FoldoutGroup("Circle Settings"), SerializeField, MinValue(3)]
    private int _pointCount = 16;

    [FoldoutGroup("Circle Settings"), SerializeField, MinValue(0.01f)]
    private float _radius = 1f;

    private Light2D _light2D;

    private void OnValidate()
    {
        if (_light2D == null)
            _light2D = GetComponent<Light2D>();
    }

    [Button("Generate Circle Shape")]
    private void GenerateCircleShape()
    {
        if (_light2D == null)
            _light2D = GetComponent<Light2D>();

        if (_light2D.lightType != Light2D.LightType.Freeform)
        {
            DebugManager.Warning($"[Core] {name}: Light2D must be set to Freeform type to generate custom shapes.");
            return;
        }

        _light2D.SetShapePath(CreateCirclePoints(_pointCount, _radius).ToArray());
        DebugManager.Log($"[Core] {name}: Generated Freeform Circle with {_pointCount} points, radius {_radius}");
    }

    private List<Vector3> CreateCirclePoints(int pointCount, float radius)
    {
        var points = new List<Vector3>(pointCount);
        float angleStep = 360f / pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleStep);
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            points.Add(new Vector2(x, y));
        }

        return points;
    }
}
