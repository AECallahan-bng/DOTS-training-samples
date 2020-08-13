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
        var content = GetSingleton<FarmContent>();
        Entities
            .WithAll<AiTagCommandClear>()
            .ForEach((
            int entityInQueryIndex,
            ref Entity AiEntity,
            in AiTargetCell targetCell,
            in Translation translationComponent) =>
            {
                var curCellPosition = MapGenerationSystem.WorldToCell(translationComponent.Value, content.CellSize);
                if(math.all(curCellPosition == targetCell.CellCoords))
                {
                    //we're at the rock, time to break it!
                }
                
                
            }).ScheduleParallel();
        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }

}
