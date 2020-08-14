using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class TeleportCropsSystem : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;

    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();

        float delta = Time.DeltaTime;
        float speed = 10f;
        Entities.ForEach((
            int entityInQueryIndex,
            Entity cropEntity,
            ref CropSellingTag timedLapsed,
            ref Translation translationComponent, 
            ref NonUniformScale scaleComponent) => 
        {
            translationComponent.Value.y += timedLapsed.Value * speed;
            
            if (scaleComponent.Value.x >= 0.1f)
            {
                scaleComponent.Value.x -= delta;
                scaleComponent.Value.z -= delta;
            }
            if (timedLapsed.Value > 1f)
            {
                ecb.DestroyEntity(entityInQueryIndex, cropEntity);
            }

            timedLapsed.Value += delta;
        }).ScheduleParallel();

        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }
}
