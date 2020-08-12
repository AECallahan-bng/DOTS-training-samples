﻿using Unity.Entities;

// Stores references to content prefabs.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct FarmContent : IComponentData
{
	public Entity UntilledLand;
	public Entity TilledLand;
	public Entity Rock;
	public Entity Teleporter;

	public Entity Farmer;
	public Entity Drone;
	public Entity Crop;
	public Unity.Mathematics.int2 CellSize;
	public uint Seed;
	public int TeleporterCount;
	public float Rockthreshold;
}
