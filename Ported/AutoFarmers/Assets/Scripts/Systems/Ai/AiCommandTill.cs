using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class AiCommandTillSystem : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;
    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<BeginInitializationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
	{
		if (HasSingleton<SectionWorldTag>())
		{
			var ecb = new EntityCommandBuffer(Allocator.Temp);

			FarmContent farmContent = GetSingleton<FarmContent>();
			Entity mapEntity = GetSingletonEntity<SectionWorldTag>();
			GridSize gridSize = GetSingleton<GridSize>();

			var getChildBuffer = GetBufferFromEntity<Child>();
			var getWorldGrid = GetBufferFromEntity<SectionWorldGrid>();

			Entities.WithAll<AiTagCommandTill>()
				.WithNativeDisableContainerSafetyRestriction(getWorldGrid)
				.WithNativeDisableParallelForRestriction(getChildBuffer)
				.WithReadOnly(getWorldGrid)
				.WithReadOnly(getChildBuffer)
				.WithStructuralChanges()
				.ForEach((
				int entityInQueryIndex,
				ref Entity farmerEntity,
				in AiTargetCell targetCell,
				in Translation translation) =>
			{
				int2 pos = new int2((int)translation.Value.x, (int)translation.Value.z);

				if (pos.Equals(targetCell.CellCoords))
				{
					Entity tilledLandEntity = ecb.Instantiate(farmContent.TilledLand);

					ecb.RemoveComponent<AiTagCommandPlant>(farmerEntity);
					ecb.AddComponent<AiTagCommandIdle>(farmerEntity);

					int mapIndex = targetCell.CellCoords.y * gridSize.Width + targetCell.CellCoords.x;
					DynamicBuffer<SectionWorldGrid> worldGrid = getWorldGrid[mapEntity];
					Entity cellEntity = worldGrid[mapIndex].Value;
					ecb.RemoveComponent<CellTagUntilledGround>(cellEntity);
					ecb.AddComponent<CellTagTilledGround>(cellEntity);
					DynamicBuffer<Child> childrenBuffer = getChildBuffer[cellEntity];
					for (int childIndex = 0; childIndex < childrenBuffer.Length; ++childIndex)
					{
						ecb.RemoveComponent<LocalToParent>(childrenBuffer[childIndex].Value);
					}
					ecb.AppendToBuffer(cellEntity, new Child { Value = tilledLandEntity });
				}
			}).Run();

			ecb.Playback(EntityManager);
			ecb.Dispose();
		}
    }
}