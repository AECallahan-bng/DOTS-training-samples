using Unity.Entities;

// Stores a fixed amount of resources required to spawn a single farmer.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct FarmerCost : IComponentData
{
	public int Value;
}
