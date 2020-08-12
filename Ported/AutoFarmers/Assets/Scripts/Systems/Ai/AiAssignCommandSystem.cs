using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class AiAssignCommandSystem : SystemBase
{
	private EntityQuery _queryRocks;
	private EntityQuery _queryCrops;
	private EntityQuery _queryTilledLand;
	private EntityQuery _queryUntilledLand;
	private AiProcessCommandRequestSystem _commandBufferSystem;

	protected override void OnCreate()
	{
		_queryRocks = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[] {
				ComponentType.ReadOnly<RockHealth>(),
				ComponentType.ReadOnly<CellPosition>(),
			},
			None = new ComponentType[] {
				ComponentType.ReadOnly<AssignedAi>(),
			}
		});

		_queryCrops = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[] {
				ComponentType.ReadOnly<CellTagGrownCrop>(),
				ComponentType.ReadOnly<CellPosition>(),
			},
			None = new ComponentType[] {
				ComponentType.ReadOnly<AssignedAi>(),
			}
		});

		_queryTilledLand = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[] {
				ComponentType.ReadOnly<CellTagTilledGround>(),
				ComponentType.ReadOnly<CellPosition>(),
			},
			None = new ComponentType[] {
				ComponentType.ReadOnly<AssignedAi>(),
			}
		});

		_queryUntilledLand = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[] {
				ComponentType.ReadOnly<CellTagUntilledGround>(),
				ComponentType.ReadOnly<CellPosition>(),
			},
			None = new ComponentType[] {
				ComponentType.ReadOnly<AssignedAi>(),
			}
		});

		_commandBufferSystem = World.GetExistingSystem<AiProcessCommandRequestSystem>();
	}

    protected override void OnUpdate()
	{
		const float cAutoCropDistance = 10.0f;
		const float cAutoRockDistance = 10.0f;
		const float cAutoPlantDistance = 10.0f;

		NativeArray<CellPosition> rocks = _queryRocks.ToComponentDataArrayAsync<CellPosition>(Allocator.TempJob, out JobHandle rockJobHandle);
		NativeArray<CellPosition> crops = _queryCrops.ToComponentDataArrayAsync<CellPosition>(Allocator.TempJob, out JobHandle cropJobHandle);
		NativeArray<CellPosition> tilledLand = _queryTilledLand.ToComponentDataArrayAsync<CellPosition>(Allocator.TempJob, out JobHandle tilledLandJobHandle);
		NativeArray<CellPosition> untilledLand = _queryUntilledLand.ToComponentDataArrayAsync<CellPosition>(Allocator.TempJob, out JobHandle untilledLandJobHandle);

		Dependency = JobHandle.CombineDependencies(Dependency, rockJobHandle);
		Dependency = JobHandle.CombineDependencies(Dependency, cropJobHandle);
		Dependency = JobHandle.CombineDependencies(Dependency, tilledLandJobHandle);
		Dependency = JobHandle.CombineDependencies(Dependency, untilledLandJobHandle);

		var commandBuffer = _commandBufferSystem.CreateCommandBuffer().ToConcurrent();

		Entities.WithAll<AiTagCommandIdle>().ForEach((int entityInQueryIndex, Entity aiEntity, ref Translation aiPosition) => {

			int2 aiCellPosition = new int2((int)aiPosition.Value.x, (int)aiPosition.Value.z);
			AiCommands closestType = AiCommands.Idle;
			AiCommands selectedCommand = AiCommands.Idle;
			int2 closestPosition = default;
			float closestDistanceSq = float.MaxValue;

			AiAssignCommandSystem.FindClosestCell(ref crops, ref aiCellPosition, out int closestCropIndex, out float closestCropDistanceSq);
			if (closestCropDistanceSq < cAutoCropDistance * cAutoCropDistance)
			{
				selectedCommand = AiCommands.Pick;
				closestPosition = crops[closestCropIndex].Value;
			}
			closestType = AiCommands.Pick;
			closestDistanceSq = closestCropDistanceSq;

			if (selectedCommand == AiCommands.Idle)
			{
				AiAssignCommandSystem.FindClosestCell(ref rocks, ref aiCellPosition, out int closestRockIndex, out float closestRockDistanceSq);
				if (closestRockDistanceSq < cAutoRockDistance * cAutoRockDistance)
				{
					selectedCommand = AiCommands.Clear;
					closestPosition = rocks[closestRockIndex].Value;
				}
				if (closestRockDistanceSq < closestDistanceSq)
				{
					closestDistanceSq = closestRockDistanceSq;
					closestType = AiCommands.Clear;
					closestPosition = rocks[closestRockIndex].Value;
				}
			}

			if (selectedCommand == AiCommands.Idle)
			{
				AiAssignCommandSystem.FindClosestCell(ref tilledLand, ref aiCellPosition, out int closestTilledLandIndex, out float closestTilledLandDistanceSq);
				if (closestTilledLandDistanceSq < cAutoPlantDistance * cAutoPlantDistance)
				{
					selectedCommand = AiCommands.Plant;
					closestPosition = tilledLand[closestTilledLandIndex].Value;
				}
				if (closestTilledLandDistanceSq < closestDistanceSq)
				{
					closestDistanceSq = closestTilledLandDistanceSq;
					closestType = AiCommands.Plant;
					closestPosition = tilledLand[closestTilledLandIndex].Value;
				}
			}

			if (selectedCommand == AiCommands.Idle)
			{
				AiAssignCommandSystem.FindClosestCell(ref untilledLand, ref aiCellPosition, out int closestUntilledLandIndex, out float closestUntilledLandDistanceSq);
				if (closestUntilledLandDistanceSq < closestDistanceSq)
				{
					closestDistanceSq = closestUntilledLandDistanceSq;
					closestType = AiCommands.Till;
					closestPosition = untilledLand[closestUntilledLandIndex].Value;
				}
				selectedCommand = closestType;
			}

			if (selectedCommand != AiCommands.Idle)
			{
				Entity commandEntity = commandBuffer.CreateEntity(entityInQueryIndex);
				commandBuffer.AddComponent<AiCommandRequest>(entityInQueryIndex, commandEntity, new AiCommandRequest
				{
					RequestedAi = aiEntity,
					TargetPosition = closestPosition,
					CommandType = selectedCommand
				});
			}

		}).ScheduleParallel();

		_commandBufferSystem.AddJobHandleForProducer(this.Dependency);
	}

	static void FindClosestCell(ref NativeArray<CellPosition> cells, ref int2 testPosition, out int closestIndex, out float closestDistanceSq)
	{
		closestDistanceSq = float.MaxValue;
		closestIndex = 0;
		for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
		{
			int2 deltaPosition = cells[cellIndex].Value - testPosition;
			float distanceSq = deltaPosition.x * deltaPosition.x + deltaPosition.y * deltaPosition.y;
			if (distanceSq < closestDistanceSq)
			{
				distanceSq = closestDistanceSq;
				closestIndex = cellIndex;
			}
		}
	}
}
