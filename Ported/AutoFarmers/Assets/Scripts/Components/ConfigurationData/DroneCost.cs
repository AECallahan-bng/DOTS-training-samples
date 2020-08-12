using Unity.Entities;

// Stores a fixed amount of resources required to spawn a single drone.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct DroneCost : IComponentData
{
	public int Value;
}
