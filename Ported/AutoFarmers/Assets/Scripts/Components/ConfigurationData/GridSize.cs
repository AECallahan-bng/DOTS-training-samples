using Unity.Entities;

// Stores the size of grid in cells to be spawned at world generation.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct GridSize : IComponentData
{
	public int Width;
	public int Height;
}
