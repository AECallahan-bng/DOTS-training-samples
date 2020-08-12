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

        var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithAll<AiTagCommandClear>()
            .ForEach((
            int entityInQueryIndex,
            ref Entity AiEntity,
            in Translation translationComponent) =>
            {

                //var worldEntity = GetSingletonEntity<SectionWorldTag>();
                //var map = GetBuffer<SectionWorldGrid>(worldEntity);
                
            }).ScheduleParallel();
        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }

}
