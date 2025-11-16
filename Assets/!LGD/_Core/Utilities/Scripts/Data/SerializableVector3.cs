using System;
using UnityEngine;

/// <summary>
/// Simple serializable position struct (avoids Vector3 JSON circular reference issues)
/// </summary>
[Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public SerializableVector3(Vector3 vector)
    {
        this.x = vector.x;
        this.y = vector.y;
        this.z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public static implicit operator Vector3(SerializableVector3 s)
    {
        return s.ToVector3();
    }

    public static implicit operator SerializableVector3(Vector3 v)
    {
        return new SerializableVector3(v);
    }

    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }
}