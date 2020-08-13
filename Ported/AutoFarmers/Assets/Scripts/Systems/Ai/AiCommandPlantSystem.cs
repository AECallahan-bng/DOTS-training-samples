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

        FarmContent farmContent = GetSingleton<FarmContent>();

        Entities.WithAll<AiTagCommandPlant>().ForEach((
            int entityInQueryIndex, 
            ref Entity farmerEntity, 
            in AiTargetCell targetCell, 
            in Translation translation) => 
        {
            int2 pos = new int2((int)translation.Value.x, (int)translation.Value.z);

            if (pos.Equals(targetCell.CellCoords))
            {
                Entity cropEntity = ecb.Instantiate(entityInQueryIndex, farmContent.Crop);
                ecb.AddComponent(entityInQueryIndex, cropEntity, new CropGrowth { Value = 5 });
                ecb.SetComponent(entityInQueryIndex, cropEntity, translation);

                ecb.RemoveComponent<AiTagCommandPlant>(entityInQueryIndex, farmerEntity);
                ecb.AddComponent<AiTagCommandIdle>(entityInQueryIndex, farmerEntity);
            }
        }).ScheduleParallel();

        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }
}
