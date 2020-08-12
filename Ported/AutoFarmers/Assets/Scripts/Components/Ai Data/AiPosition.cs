using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct AiPosition : IComponentData
{
	float3 Value;
}
