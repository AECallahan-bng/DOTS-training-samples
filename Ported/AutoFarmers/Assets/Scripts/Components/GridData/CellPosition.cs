using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct CellPosition : IComponentData
{
	int2 Value;
}
