using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class AiCommandClear : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;

    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
    }

}
