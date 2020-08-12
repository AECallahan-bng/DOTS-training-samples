using Unity.Entities;
using Unity.Mathematics;

// Stores an accumulator of resources used for spawning drones.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct DroneResources : IComponentData
{
	public int Resources;
	public int2 LastGridPosition;
}
