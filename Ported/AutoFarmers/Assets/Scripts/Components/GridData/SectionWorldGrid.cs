using Unity.Entities;


public struct SectionWorldGrid : IBufferElementData
{
    public Entity Value;
}
public struct Ground : IComponentData
{
    public Entity Value;

}
public struct Over : IComponentData
{
    public Entity Value;

}