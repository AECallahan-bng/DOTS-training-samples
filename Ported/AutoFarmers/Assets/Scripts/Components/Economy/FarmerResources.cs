using Unity.Entities;
using Unity.Mathematics;

// Stores an accumulator of resources used for spawning farmers.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct FarmerResources : IComponentData
{
	public int Resources;
	public int2 LastGridPosition;
}
