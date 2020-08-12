using Unity.Burst;
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
	protected override void OnCreate()
	{
		_entityCommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	private EntityCommandBufferSystem _entityCommandBufferSystem;

	protected override void OnUpdate()
	{
		var commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
		// now process the requests
		Entities.ForEach((int entityInQueryIndex, Entity aiEntity, ref AiCommandRequest request) => {
			commandBuffer.DestroyEntity(entityInQueryIndex, aiEntity);

			commandBuffer.RemoveComponent<AiTagCommandIdle>(entityInQueryIndex, request.RequestedAi);
			commandBuffer.SetComponent(entityInQueryIndex, request.RequestedAi, new AiTargetCell
			{
				CellCoords = request.TargetPosition
			});


			switch (request.CommandType)
			{
				case AiCommands.Clear:
					commandBuffer.AddComponent<AiTagCommandClear>(entityInQueryIndex, request.RequestedAi);
					break;

				case AiCommands.Pick:
					commandBuffer.AddComponent<AiTagCommandPick>(entityInQueryIndex, request.RequestedAi);
					break;

				case AiCommands.Plant:
					commandBuffer.AddComponent<AiTagCommandPlant>(entityInQueryIndex, request.RequestedAi);
					break;

				case AiCommands.Till:
					commandBuffer.AddComponent<AiTagCommandTill>(entityInQueryIndex, request.RequestedAi);
					break;

				case AiCommands.Sell:
					commandBuffer.AddComponent<AiTagCommandSell>(entityInQueryIndex, request.RequestedAi);
					break;
			}
		}).ScheduleParallel();

		_entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);

	}
}
