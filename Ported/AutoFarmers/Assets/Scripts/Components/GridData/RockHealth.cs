using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct RockHealth : IComponentData
{
	public int Value;
}
