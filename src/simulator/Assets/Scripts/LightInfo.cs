using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LightInfo 
{
    public bool active;
    public string objectName;
    public string objectPrefabName;
    public Vector3 position;
    public Vector3 rotation;
    public Color color;
    public float intensity;
}
