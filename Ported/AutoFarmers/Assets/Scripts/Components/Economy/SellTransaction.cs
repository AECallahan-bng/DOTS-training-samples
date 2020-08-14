﻿using Unity.Entities;
using Unity.Mathematics;

// Transient storage of resources when a crop is sold.
[GenerateAuthoringComponent]
struct SellTransaction : IComponentData
{
	public int Resources;
	public int2 GridPosition;
}