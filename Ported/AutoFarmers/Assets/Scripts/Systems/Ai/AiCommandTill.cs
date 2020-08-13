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
			
			var getWorldGrid = GetBufferFromEntity<SectionWorldGrid>();

			Entities.WithAll<AiTagCommandTill>()
				.WithNativeDisableContainerSafetyRestriction(getWorldGrid)
				.WithReadOnly(getWorldGrid)
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

					Ground existingGround = GetComponent<Ground>(cellEntity);
					if (existingGround.Value != Entity.Null)
					{
						ecb.DestroyEntity(existingGround.Value);
					}
					ecb.SetComponent<Ground>(cellEntity, new Ground() { Value = tilledLandEntity });
					ecb.SetComponent<Translation>(tilledLandEntity, translation);
				}
			}).Run();

			ecb.Playback(EntityManager);
			ecb.Dispose();
		}
    }
}