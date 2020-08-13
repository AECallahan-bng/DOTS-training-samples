using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class AiCommandPlantSystem : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;
    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();

        if (HasSingleton<SectionWorldTag>())
        {
            var map = GetSingletonEntity<SectionWorldTag>();
            var buffer = GetBuffer<SectionWorldGrid>(map);
            var size = GetSingleton<GridSize>();
            int2 sizeInt = new int2(size.Width, size.Height);

            FarmContent farmContent = GetSingleton<FarmContent>();

            Entities
                .WithAll<AiTagCommandPlant>()
                .WithNativeDisableParallelForRestriction(buffer)
                .ForEach((
                int entityInQueryIndex,
                ref Entity farmerEntity,
                in AiTargetCell targetCell,
                in Translation translation) =>
            {
                int2 pos = new int2((int)translation.Value.x, (int)translation.Value.z);
                int bufferIndex = PosToIndex(sizeInt, targetCell.CellCoords);
                Entity cellEntity = buffer[bufferIndex].Value;

                if (pos.Equals(targetCell.CellCoords))
                {
                    Entity cropEntity = ecb.Instantiate(entityInQueryIndex, farmContent.Crop);
                    ecb.AddComponent(entityInQueryIndex, cropEntity, new CropGrowth { Value = 5 });
                    ecb.SetComponent(entityInQueryIndex, cropEntity, translation);

                    ecb.AddComponent(entityInQueryIndex, cellEntity, new Over { Value = cropEntity });

                    ecb.RemoveComponent<CellTagTilledGround>(entityInQueryIndex, cellEntity);
                    ecb.AddComponent<CellTagPlantedGround>(entityInQueryIndex, cellEntity);
                    ecb.RemoveComponent<AssignedAi>(entityInQueryIndex, cellEntity);

                    ecb.RemoveComponent<AiTagCommandPlant>(entityInQueryIndex, farmerEntity);
                    ecb.AddComponent<AiTagCommandIdle>(entityInQueryIndex, farmerEntity);
                }
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
