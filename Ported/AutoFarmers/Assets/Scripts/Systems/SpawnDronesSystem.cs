using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class SpawnDronesSystem : SystemBase
{
	private Random m_Random;

    public int CurrentCount = 0;
    protected override void OnCreate()
	{
		m_Random = new Random(0x1234567);
	}

	protected override void OnUpdate()
	{
		var ecb = new EntityCommandBuffer(Allocator.TempJob);
		var random = m_Random;
		var createdCount = 0;
		Entities.WithName("Spawn_Drones").ForEach((ref DroneResources resources, in DroneCost droneCost, in FarmContent farmContent) =>
		{
			int newResources = resources.Resources;

			while (newResources >= droneCost.Cost)
			{
				float3 spawnCenter = new float3(resources.LastGridPosition.x + 0.5f, 1.0f, resources.LastGridPosition.y + 0.5f);

				for (int i = 0; i < droneCost.SpawnCount; i++)
				{
					Entity droneEntity = ecb.Instantiate(farmContent.Drone);
					float3 offset = random.NextFloat3(new float3(-0.4f, 0.0f, -0.4f), new float3(+0.4f, 0.0f, +0.4f));
					float3 spawnPosition = spawnCenter + offset;

					ecb.SetComponent(droneEntity, new Unity.Transforms.Translation() { Value = spawnPosition });
					ecb.AddComponent(droneEntity, new MovePosition() { Value = spawnPosition });
					ecb.AddComponent<AiTagDrone>(droneEntity);
					ecb.AddComponent<AiTagCommandIdle>(droneEntity);
					ecb.AddComponent<AiTargetCell>(droneEntity);
				}
				newResources -= droneCost.Cost;
				++createdCount;
			}

			resources.Resources = newResources;
		}).Run();
		ecb.Playback(EntityManager);
		ecb.Dispose();
		CurrentCount += createdCount;
		m_Random = random;
	}
}

