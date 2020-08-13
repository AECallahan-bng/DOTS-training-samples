using Unity.Entities;

// Stores the size of grid in cells to be spawned at world generation.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct GridSize : IComponentData
{
	public int Width;
	public int Height;
	public Unity.Mathematics.int2 Value
	{
		get
		{
			return new Unity.Mathematics.int2(Width, Height);
		}
	}
}
