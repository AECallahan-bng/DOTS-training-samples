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
			var getChildBuffer = GetBufferFromEntity<Child>(true);

			Entities.WithAll<AiTagCommandPick>()
				.WithNativeDisableParallelForRestriction(buffer)
				.WithNativeDisableParallelForRestriction(getChildBuffer)
				.WithReadOnly(buffer)
				.WithReadOnly(getChildBuffer)
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
                    var childBuffer = getChildBuffer[entityInPos];
                    
                    for (int childIndex = 0; childIndex < childBuffer.Length; ++childIndex)
                    {
                        if (HasComponent<FullGrownCropTag>(childBuffer[childIndex].Value))
                        {
                            Entity crop = childBuffer[childIndex].Value;
                            ecb.RemoveComponent<Parent>(entityInQueryIndex, crop);
                            ecb.AddComponent(entityInQueryIndex, aiEntity, new AiCarriedObject { CarriedObjectEntity = crop });
                            ecb.AddComponent<AiObjectBeingCarried>(entityInQueryIndex, crop);
                        }
                    }

                    ecb.RemoveComponent<CellTagGrownCrop>(entityInQueryIndex, aiEntity);

                    ecb.RemoveComponent<CellTagPlantedGround>(entityInQueryIndex, aiEntity);
                    ecb.AddComponent<CellTagTilledGround>(entityInQueryIndex, aiEntity);

                    ecb.RemoveComponent<AiTagCommandPick>(entityInQueryIndex, aiEntity);
                    ecb.AddComponent<AiTagCommandIdle>(entityInQueryIndex, aiEntity);

                    //ecb.SetBuffer<Child>(entityInQueryIndex, tile);
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
