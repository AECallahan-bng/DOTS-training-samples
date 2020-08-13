﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(AiAssignCommandSystem))]
public class AiProcessCommandRequestSystem : EntityCommandBufferSystem
{
}

[UpdateAfter(typeof(AiProcessCommandRequestSystem))]
public class AiProcessCommandRequestPostSystem : SystemBase
{
	private EntityQuery _requestQuery;
	private EntityCommandBufferSystem _entityCommandBufferSystem;

	protected override void OnCreate()
	{
		_requestQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<AiCommandRequest>()
			}
		});

		_entityCommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate()
	{
		var commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
		if (HasSingleton<SectionWorldTag>())
		{
			var map = GetSingletonEntity<SectionWorldTag>();
			var buffer = GetBuffer<SectionWorldGrid>(map);
			var size = GetSingleton<GridSize>();
			int2 sizeInt = new int2(size.Width, size.Height);

			NativeArray<Entity> requestEntities = _requestQuery.ToEntityArrayAsync(Allocator.TempJob, out var entitiesArrayHandle);
			NativeArray<AiCommandRequest> requests = _requestQuery.ToComponentDataArrayAsync<AiCommandRequest>(Allocator.TempJob, out JobHandle requestsArrayJobHandle);

			Dependency = JobHandle.CombineDependencies(Dependency, entitiesArrayHandle);
			Dependency = JobHandle.CombineDependencies(Dependency, requestsArrayJobHandle);

			// now process the requests
			Entities
				.WithDisposeOnCompletion(requestEntities)
				.WithDisposeOnCompletion(requests)
				.WithNativeDisableParallelForRestriction(buffer)
				.ForEach((int entityInQueryIndex, Entity worldEntity, in SectionWorldTag worldTag) =>
				{
					var processedRequests = new NativeHashSet<int2>(requests.Length, Allocator.Temp);

					for (int i = 0; i < requests.Length; i++)
					{
						bool IsExclusiveRequest = (requests[i].CommandType != AiCommands.Sell);

						commandBuffer.DestroyEntity(entityInQueryIndex, requestEntities[i]);

						if (!IsExclusiveRequest || processedRequests.Add(requests[i].TargetPosition))
						{
							int bufferIndex = PosToIndex(sizeInt, requests[i].TargetPosition);
							Entity cellEntity = buffer[bufferIndex].Value;

							commandBuffer.RemoveComponent<AiTagCommandIdle>(entityInQueryIndex, requests[i].RequestedAi);
							commandBuffer.SetComponent(entityInQueryIndex, requests[i].RequestedAi, new AiTargetCell
							{
								CellCoords = requests[i].TargetPosition
							});
							if (requests[i].CommandType != AiCommands.Sell)
							{ 
								commandBuffer.AddComponent(entityInQueryIndex, cellEntity, new AssignedAi { Ai = requests[i].RequestedAi }); 
							}

							switch (requests[i].CommandType)
							{
								case AiCommands.Clear:
									commandBuffer.AddComponent<AiTagCommandClear>(entityInQueryIndex, requests[i].RequestedAi);
									break;

								case AiCommands.Pick:
									commandBuffer.AddComponent<AiTagCommandPick>(entityInQueryIndex, requests[i].RequestedAi);
									break;

								case AiCommands.Plant:
									commandBuffer.AddComponent<AiTagCommandPlant>(entityInQueryIndex, requests[i].RequestedAi);
									break;

								case AiCommands.Till:
									commandBuffer.AddComponent<AiTagCommandTill>(entityInQueryIndex, requests[i].RequestedAi);
									break;

								case AiCommands.Sell:
									commandBuffer.AddComponent<AiTagCommandSell>(entityInQueryIndex, requests[i].RequestedAi);
									break;
							}
						}
					}

					processedRequests.Dispose();
				}
			).ScheduleParallel();
		}
		_entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
	}
	static int PosToIndex(int2 size, int2 pos)
	{
		int i = pos.y * size.x + pos.x;
		return i;
	}
}
