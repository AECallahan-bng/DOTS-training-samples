using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class AiCommandPickSystem : SystemBase
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
            var query = GetEntityQuery(typeof(SectionWorldGrid));
            var buffer = GetBuffer<SectionWorldGrid>(query.GetSingletonEntity());
            var size = GetSingleton<GridSize>();
            int2 sizeInt = new int2(size.Width, size.Height);

            FarmContent farmContent = GetSingleton<FarmContent>();

            Entities.WithAll<AiTagCommandPick>()
                .WithNativeDisableParallelForRestriction(buffer)
                .ForEach((
                int entityInQueryIndex,
                ref Entity aiEntity,
                in AiTargetCell targetCell,
                in Translation translation) =>
            {
                int2 pos = new int2((int)translation.Value.x, (int)translation.Value.z);
                int bufferIndex = PosToIndex(sizeInt, targetCell.CellCoords);
                Entity entityInPos = buffer[bufferIndex].Value;

                if (pos.Equals(targetCell.CellCoords))
                {
                    var childBuffer = GetBuffer<Child>(entityInPos);
                    for (int childIndex = 0; childIndex < childBuffer.Length; ++childIndex)
                    {
                        
                    }
                    

                    ecb.RemoveComponent<CellTagGrownCrop>(entityInQueryIndex, entityInPos);
                    ecb.RemoveComponent<CellTagPlantedGround>(entityInQueryIndex, entityInPos);
                    ecb.AddComponent<CellTagTilledGround>(entityInQueryIndex, entityInPos);

                    ecb.RemoveComponent<AiTagCommandPick>(entityInQueryIndex, aiEntity);
                    ecb.AddComponent<AiTagCommandIdle>(entityInQueryIndex, aiEntity);
                    ecb.AddComponent(entityInQueryIndex, aiEntity, new AiCarriedObject { CarriedObjectEntity = entityInPos });

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
