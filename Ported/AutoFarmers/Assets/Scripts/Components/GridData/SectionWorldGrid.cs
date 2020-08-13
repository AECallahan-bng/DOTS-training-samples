using Unity.Entities;


public struct SectionWorldGrid : IBufferElementData
{
	public Entity Cell;
	public Entity Ground;
	public Entity Over;
}