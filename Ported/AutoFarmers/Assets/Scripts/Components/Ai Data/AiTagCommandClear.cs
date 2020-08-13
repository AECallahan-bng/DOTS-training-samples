using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct AiTagCommandClear : IComponentData
{
    public float AnimationTime;
    public bool IsBreaking;
}
