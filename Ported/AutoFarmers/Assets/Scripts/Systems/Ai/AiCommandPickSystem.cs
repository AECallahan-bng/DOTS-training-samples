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
                Entity cellEntity = buffer[bufferIndex].Value;

                if (pos.Equals(targetCell.CellCoords))
                {
                    var childBuffer = getChildBuffer[cellEntity];
					var newChildBuffer = ecb.SetBuffer<Child>(entityInQueryIndex, cellEntity);

					// iterate through all children of the cell, removing a fully-grown crop if we found one,
					// and leaving all other children in the array (by adding them to the new DynamicBuffer that we
					// are going to set as the child array)
					for (int childIndex = 0; childIndex < childBuffer.Length; ++childIndex)
                    {
                        if (HasComponent<FullGrownCropTag>(childBuffer[childIndex].Value))
                        {
                            Entity crop = childBuffer[childIndex].Value;
                            ecb.RemoveComponent<Parent>(entityInQueryIndex, crop);
							ecb.RemoveComponent<LocalToParent>(entityInQueryIndex, crop);
							ecb.AddComponent(entityInQueryIndex, aiEntity, new AiCarriedObject { CarriedObjectEntity = crop });
                            ecb.AddComponent(entityInQueryIndex, crop, new AiObjectBeingCarried { CarrierEntity = aiEntity });
                        }
						else
						{
							newChildBuffer.Add(new Child() { Value = childBuffer[childIndex].Value });
						}
                    }

                    ecb.RemoveComponent<CellTagGrownCrop>(entityInQueryIndex, cellEntity);
                    ecb.AddComponent<CellTagTilledGround>(entityInQueryIndex, cellEntity);

                    ecb.RemoveComponent<AiTagCommandPick>(entityInQueryIndex, aiEntity);
                    ecb.AddComponent<AiTagCommandIdle>(entityInQueryIndex, aiEntity);
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
