using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class AiCommandTillSystem : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;
    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
	{
		UnityEngine.Debug.Log("Running Command Till");

		if (HasSingleton<SectionWorldTag>())
		{
			var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();

			FarmContent farmContent = GetSingleton<FarmContent>();
			Entity mapEntity = GetSingletonEntity<SectionWorldTag>();
			DynamicBuffer<SectionWorldGrid> worldGrid = GetBuffer<SectionWorldGrid>(mapEntity);
			GridSize gridSize = GetSingleton<GridSize>();

			var getChildBuffer = GetBufferFromEntity<Child>(true);

			Entities.WithAll<AiTagCommandTill>().WithNativeDisableContainerSafetyRestriction(worldGrid).ForEach((
				int entityInQueryIndex,
				ref Entity farmerEntity,
				in AiTargetCell targetCell,
				in Translation translation) =>
			{
				int2 pos = new int2((int)translation.Value.x, (int)translation.Value.z);

				if (pos.Equals(targetCell.CellCoords))
				{
					Entity tilledLandEntity = ecb.Instantiate(entityInQueryIndex, farmContent.TilledLand);
					ecb.AddComponent(entityInQueryIndex, tilledLandEntity, new CellTagTilledGround());

					ecb.RemoveComponent<AiTagCommandPlant>(entityInQueryIndex, farmerEntity);
					ecb.AddComponent<AiTagCommandIdle>(entityInQueryIndex, farmerEntity);

					int mapIndex = targetCell.CellCoords.y * gridSize.Width + targetCell.CellCoords.x;
					Entity cellEntity = worldGrid[mapIndex].Value;
					DynamicBuffer<Child> childrenBuffer = getChildBuffer[cellEntity];
					for (int childIndex = 0; childIndex < childrenBuffer.Length; ++childIndex)
					{
						ecb.DestroyEntity(entityInQueryIndex, childrenBuffer[childIndex].Value);
					}
					ecb.AppendToBuffer(entityInQueryIndex, cellEntity, new Child { Value = tilledLandEntity });
				}
			}).ScheduleParallel();

			m_ECBSystem.AddJobHandleForProducer(Dependency);
		}
    }
}
