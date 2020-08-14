using Unity.Entities;

// Stores references to content prefabs.
// Present on Economy singleton only.
[GenerateAuthoringComponent]
struct FarmContent : IComponentData
{
	public Entity UntilledLand;
	public Entity TilledLand;
	public Entity Rock;
	public Entity Teleporter;

	public Entity Farmer;
	public Entity Drone;
	public Entity Crop;
	// size in real world of a cell. Should be (1,1)
	public Unity.Mathematics.float2 CellSize;
	public uint Seed;
	public int TeleporterCount;
	public bool GenerateTilled;
	public float Rockthreshold;
	public int AiRandomJitterTill;
	public int AiRandomJitterPlant;
	public int AiRandomJitterClear;
	public int AiRandomJitterPick;
	public int AiRandomJitterSell;
}
