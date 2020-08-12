using Unity.Entities;

// Stores a fixed maximum quantity of farmers that may be spawned.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct FarmerLimit : IComponentData
{
	public int Value;
}
