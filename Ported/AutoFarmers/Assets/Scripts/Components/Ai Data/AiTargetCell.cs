using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct AiTargetCell : IComponentData
{
	int2 CellCoords;
}
