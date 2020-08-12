using Unity.Entities;

// Stores a fixed amount of resources required to spawn a group of drones.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct DroneCost : IComponentData
{
	public int Cost;
	public int SpawnCount;
}
