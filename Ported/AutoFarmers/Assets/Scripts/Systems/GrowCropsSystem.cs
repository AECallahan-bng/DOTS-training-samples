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
        if (HasSingleton<SectionWorldTag>())
        {
            var map = GetSingletonEntity<SectionWorldTag>();
            var buffer = GetBuffer<SectionWorldGrid>(map).AsNativeArray();
            var size = GetSingleton<GridSize>();
            int2 sizeInt = new int2(size.Width, size.Height);

            Entities
                .WithNativeDisableContainerSafetyRestriction(buffer)
                .WithReadOnly(buffer)
                .ForEach((
            int entityInQueryIndex,
            Entity cropEntity,
            ref CropGrowth growthComponent,
            ref NonUniformScale scale,
            in Translation translation) =>
        {
            int2 pos = new int2((int)translation.Value.x, (int)translation.Value.z);
            int bufferIndex = PosToIndex(sizeInt, pos);
            Entity cellEntity = buffer[bufferIndex].Value;

            float3 currentScale = scale.Value;
            currentScale.x = math.lerp(1, currentScale.x, math.clamp(growthComponent.Value, 0, 1));
            currentScale.y = math.lerp(1, currentScale.y, math.clamp(growthComponent.Value, 0, 1));
            currentScale.z = math.lerp(1, currentScale.z, math.clamp(growthComponent.Value, 0, 1));
            growthComponent.Value -= delta;

            if (growthComponent.Value <= 0)
            {
                ecb.RemoveComponent<CropGrowth>(entityInQueryIndex, cropEntity);
                ecb.AddComponent<FullGrownCropTag>(entityInQueryIndex, cropEntity);

                ecb.RemoveComponent<CellTagPlantedGround>(entityInQueryIndex, cellEntity);
                ecb.AddComponent<CellTagGrownCrop>(entityInQueryIndex, cellEntity);
            }
            scale.Value = currentScale;
        }).ScheduleParallel();
        }
        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }
    static int PosToIndex(int2 size, int2 pos)
    {
        int i = pos.y * size.x + pos.x;
        return i;
    }
}
