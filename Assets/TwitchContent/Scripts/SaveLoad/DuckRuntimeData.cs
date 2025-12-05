using System;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class DuckRuntimeData
{
    public string blueprintId;
    public string duckOwner;
    public float duckHSVValue;
    [JsonIgnore]
    public GameObject objRef;
}

