using Unity.Entities;

// Stores the fixed maximum quantity of rocks that may be spawned at world generation.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct RockLimit : IComponentData
{
	public int Value;
}
