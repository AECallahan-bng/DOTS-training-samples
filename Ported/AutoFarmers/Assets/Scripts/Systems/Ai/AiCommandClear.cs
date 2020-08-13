using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


public struct RockDamageReport : IBufferElementData
{
	public float Damage;
}

public class AiCommandClearSystem : SystemBase
{
	protected override void OnUpdate()
	{
		if (HasSingleton<SectionWorldTag>())
		{
			var ecb = new EntityCommandBuffer(Allocator.Temp);

			FarmContent farmContent = GetSingleton<FarmContent>();
			Entity mapEntity = GetSingletonEntity<SectionWorldTag>();
			GridSize gridSize = GetSingleton<GridSize>();

			var getWorldGrid = GetBufferFromEntity<SectionWorldGrid>();

			float deltaTime = Time.DeltaTime;

			Entities.WithAll<AiTagCommandClear>()
				.WithNativeDisableContainerSafetyRestriction(getWorldGrid)
				.WithReadOnly(getWorldGrid)
				.WithStructuralChanges()
				.ForEach((
				int entityInQueryIndex,
				ref Entity farmerEntity,
				in AiTargetCell targetCell,
				in Translation translation
			) =>
			{
				int2 pos = new int2((int)translation.Value.x, (int)translation.Value.z);

				if (pos.Equals(targetCell.CellCoords))
				{
					ecb.RemoveComponent<AiTagCommandClear>(farmerEntity);
					ecb.AddComponent<AiTagCommandIdle>(farmerEntity);

					int mapIndex = targetCell.CellCoords.y * gridSize.Width + targetCell.CellCoords.x;
					DynamicBuffer<SectionWorldGrid> worldGrid = getWorldGrid[mapEntity];
					Entity cellEntity = worldGrid[mapIndex].Value;
					ecb.RemoveComponent<AssignedAi>(cellEntity);
					ecb.AppendToBuffer<RockDamageReport>(cellEntity, new RockDamageReport { Damage = deltaTime });
				}
			}).Run();

			ecb.Playback(EntityManager);
			ecb.Dispose();

			ecb = new EntityCommandBuffer(Allocator.Temp);
			Entities
				.WithNativeDisableContainerSafetyRestriction(getWorldGrid)
				.WithReadOnly(getWorldGrid)
				.WithStructuralChanges()
				.ForEach((
				int entityInQueryIndex,
				ref Entity cellEntity,
				ref RockHealth rockHealth,
				in DynamicBuffer<RockDamageReport> damageReports
			) =>
			{
				for (int damageReportIndex = 0; damageReportIndex < damageReports.Length; damageReportIndex++)
				{
					rockHealth.Value -= damageReports[damageReportIndex].Damage;
				}
				ecb.SetBuffer<RockDamageReport>(cellEntity);

				if (rockHealth.Value <= 0)
				{
					ecb.RemoveComponent<RockHealth>(cellEntity);
					ecb.AddComponent<CellTagUntilledGround>(cellEntity);

					Over existingOver = GetComponent<Over>(cellEntity);
					if (existingOver.Value != Entity.Null)
					{
						ecb.DestroyEntity(existingOver.Value);
					}
				}
				else
				{
					ecb.SetComponent<RockHealth>(cellEntity, rockHealth);
				}
			}).Run();

			ecb.Playback(EntityManager);
			ecb.Dispose();
		}
	}
}