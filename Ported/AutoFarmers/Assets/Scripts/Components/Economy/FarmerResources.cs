using Unity.Entities;

// Stores an accumulator of resources used for spawning farmers.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct FarmerResources : IComponentData
{
	public int Value;
}
