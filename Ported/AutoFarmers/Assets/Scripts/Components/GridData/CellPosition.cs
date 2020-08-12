using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct CellPosition : IComponentData
{
	int2 Value;
}
