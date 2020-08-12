using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class GrowCropsSystem : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;

    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        float delta = Time.DeltaTime;

        var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();

        Entities.ForEach((int entityInQueryIndex, Entity cropEntity, ref CropGrowth growthComponent, ref NonUniformScale scale )=>
        {
            float3 currentScale = scale.Value;
            currentScale.x = math.lerp(1, currentScale.x, math.clamp(growthComponent.Value, 0, 1));
            currentScale.y = math.lerp(1, currentScale.y, math.clamp(growthComponent.Value, 0, 1));
            currentScale.z = math.lerp(1, currentScale.z, math.clamp(growthComponent.Value, 0, 1));
            growthComponent.Value -= delta;

            if(growthComponent.Value <= 0)
            {
                //Remove cropGrowth
                ecb.RemoveComponent<CropGrowth>(entityInQueryIndex, cropEntity);
                //Add CellTagGrownCrop
                ecb.AddComponent<CellTagGrownCrop>(entityInQueryIndex, cropEntity);
            }
            scale.Value = currentScale;
        }).ScheduleParallel();

        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }
}
