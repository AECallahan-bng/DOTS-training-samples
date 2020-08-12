using Unity.Entities;

// Stores an accumulator of resources used for spawning drones.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct DroneResources : IComponentData
{
	public int Value;
}
