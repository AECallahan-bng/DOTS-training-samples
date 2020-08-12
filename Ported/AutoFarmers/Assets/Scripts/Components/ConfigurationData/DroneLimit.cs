using Unity.Entities;

// Stores a fixed maximum quantity of drones that may be spawned.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct DroneLimit : IComponentData
{
	public int Value;
}
