using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

public class AiAssignCommandSystem : SystemBase
{
	private EntityQuery _queryRocks;
	private EntityQuery _queryCrops;
	private EntityQuery _queryTeleporters;
	private EntityQuery _queryTilledLand;
	private EntityQuery _queryUntilledLand;
	private AiProcessCommandRequestSystem _commandBufferSystem;

	private Random m_Random;

	protected override void OnCreate()
	{
		m_Random = new Random(0x1234567);

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

		_queryTeleporters = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[] {
				ComponentType.ReadOnly<CellTagTeleporter>(),
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
		const float cAutoCropDistance = 20.0f;
		const float cAutoRockDistance = 4.0f;
		const float cAutoPlantDistance = 10.0f;

		NativeArray<CellPosition> rocks = _queryRocks.ToComponentDataArrayAsync<CellPosition>(Allocator.TempJob, out JobHandle rockJobHandle);
		NativeArray<CellPosition> crops = _queryCrops.ToComponentDataArrayAsync<CellPosition>(Allocator.TempJob, out JobHandle cropJobHandle);
		NativeArray<CellPosition> teleporters = _queryTeleporters.ToComponentDataArrayAsync<CellPosition>(Allocator.TempJob, out JobHandle teleporterJobHandle);
		NativeArray<CellPosition> tilledLand = _queryTilledLand.ToComponentDataArrayAsync<CellPosition>(Allocator.TempJob, out JobHandle tilledLandJobHandle);
		NativeArray<CellPosition> untilledLand = _queryUntilledLand.ToComponentDataArrayAsync<CellPosition>(Allocator.TempJob, out JobHandle untilledLandJobHandle);

		Dependency = JobHandle.CombineDependencies(Dependency, rockJobHandle);
		Dependency = JobHandle.CombineDependencies(Dependency, cropJobHandle);
		Dependency = JobHandle.CombineDependencies(Dependency, teleporterJobHandle);
		Dependency = JobHandle.CombineDependencies(Dependency, tilledLandJobHandle);
		Dependency = JobHandle.CombineDependencies(Dependency, untilledLandJobHandle);

		var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

		var content = GetSingleton<FarmContent>();
		
		var random = m_Random;

		Entities
			.WithDisposeOnCompletion(rocks)
			.WithDisposeOnCompletion(crops)
			.WithDisposeOnCompletion(teleporters)
			.WithDisposeOnCompletion(tilledLand)
			.WithDisposeOnCompletion(untilledLand)
			.WithAll<AiTagCommandIdle>()
			.ForEach((
				int entityInQueryIndex, 
				Entity aiEntity, 
				ref Translation aiPosition) => 
		{
			int2 aiCellPosition = new int2((int)aiPosition.Value.x, (int)aiPosition.Value.z);
			random.InitState((uint)(0x1234567 ^ entityInQueryIndex ^ aiEntity.Index));

			AiCommands closestType = AiCommands.Idle;
			AiCommands selectedCommand = AiCommands.Idle;
			int2 closestPosition = default;
			float closestDistanceSq = float.MaxValue;
			bool IsFarmer = HasComponent<AiTagFarmer>(aiEntity);        // PERF: Could test this on a per-chunk basis if we use IJobChunk
			bool IsCarrying = HasComponent<AiCarriedObject>(aiEntity);

			if (selectedCommand == AiCommands.Idle && IsCarrying)
			{
				int2 jitteredCellPosition = aiCellPosition;
				jitteredCellPosition.x += random.NextInt(-content.AiRandomJitterSell, content.AiRandomJitterSell);
				jitteredCellPosition.y += random.NextInt(-content.AiRandomJitterSell, content.AiRandomJitterSell);

				AiAssignCommandSystem.FindClosestCell(ref teleporters, ref jitteredCellPosition, out int closestTeleporterIndex, out float closestTeleporterDistanceSq);

				if (closestTeleporterIndex != -1)
				{
					selectedCommand = AiCommands.Sell;
					closestPosition = teleporters[closestTeleporterIndex].Value;
					closestDistanceSq = closestTeleporterDistanceSq;
				}
			}

			if (selectedCommand == AiCommands.Idle && !IsCarrying)
			{
				int2 jitteredCellPosition = aiCellPosition;
				jitteredCellPosition.x += random.NextInt(-content.AiRandomJitterPick, content.AiRandomJitterPick);
				jitteredCellPosition.y += random.NextInt(-content.AiRandomJitterPick, content.AiRandomJitterPick);

				AiAssignCommandSystem.FindClosestCell(ref crops, ref jitteredCellPosition, out int closestCropIndex, out float closestCropDistanceSq);
				if (closestCropDistanceSq < cAutoCropDistance * cAutoCropDistance)
				{
					selectedCommand = AiCommands.Pick;
					closestPosition = crops[closestCropIndex].Value;
				}

				if (closestCropIndex != -1)
				{
					closestType = AiCommands.Pick;
					closestPosition = crops[closestCropIndex].Value;
					closestDistanceSq = closestCropDistanceSq;
				}
			}

			if (selectedCommand == AiCommands.Idle && IsFarmer && !IsCarrying)
			{
				int2 jitteredCellPosition = aiCellPosition;
				jitteredCellPosition.x += random.NextInt(-content.AiRandomJitterClear, content.AiRandomJitterClear);
				jitteredCellPosition.y += random.NextInt(-content.AiRandomJitterClear, content.AiRandomJitterClear);

				AiAssignCommandSystem.FindClosestCell(ref rocks, ref jitteredCellPosition, out int closestRockIndex, out float closestRockDistanceSq);
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

			if (selectedCommand == AiCommands.Idle && IsFarmer && !IsCarrying)
			{
				int2 jitteredCellPosition = aiCellPosition;
				jitteredCellPosition.x += random.NextInt(-content.AiRandomJitterPlant, content.AiRandomJitterPlant);
				jitteredCellPosition.y += random.NextInt(-content.AiRandomJitterPlant, content.AiRandomJitterPlant);

				AiAssignCommandSystem.FindClosestCell(ref tilledLand, ref jitteredCellPosition, out int closestTilledLandIndex, out float closestTilledLandDistanceSq);
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

			if (selectedCommand == AiCommands.Idle && IsFarmer && !IsCarrying)
			{
				int2 jitteredCellPosition = aiCellPosition;
				jitteredCellPosition.x += random.NextInt(-content.AiRandomJitterTill, content.AiRandomJitterTill);
				jitteredCellPosition.y += random.NextInt(-content.AiRandomJitterTill, content.AiRandomJitterTill);

				AiAssignCommandSystem.FindClosestCell(ref untilledLand, ref jitteredCellPosition, out int closestUntilledLandIndex, out float closestUntilledLandDistanceSq);
				if (closestUntilledLandDistanceSq < closestDistanceSq)
				{
					closestDistanceSq = closestUntilledLandDistanceSq;
					closestType = AiCommands.Till;
					closestPosition = untilledLand[closestUntilledLandIndex].Value;
				}
			}

			if (selectedCommand == AiCommands.Idle)
			{
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
		closestIndex = -1;
		for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
		{
			int2 deltaPosition = cells[cellIndex].Value - testPosition;
			float distanceSq = deltaPosition.x * deltaPosition.x + deltaPosition.y * deltaPosition.y;
			if (distanceSq < closestDistanceSq)
			{
				closestDistanceSq = distanceSq;
				closestIndex = cellIndex;
			}
		}
	}
}
