using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct AiTargetCell : IComponentData
{
	public int2 CellCoords;
}
