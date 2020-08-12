using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class SpawnFarmersSystem : SystemBase
{
	private Random m_Random;

	protected override void OnCreate()
	{
		m_Random = new Random(0x1234567);
	}

	protected override void OnUpdate()
	{
		var ecb = new EntityCommandBuffer(Allocator.TempJob);
		var random = m_Random;

		Entities.ForEach((ref FarmerResources resources, in FarmerCost farmerCost, in FarmContent farmContent) =>
		{
			int newResources = resources.Resources;

			while (newResources >= farmerCost.Value)
			{
				float3 spawnPosition = new float3(resources.LastGridPosition.x + 0.5f, 0, resources.LastGridPosition.y + 0.5f);
				// float3 spawnPosition = MapGenerationSystem::GetCellMidpoint(resources.LastGridPosition);

				var farmerEntity = ecb.Instantiate(farmContent.Farmer);
				ecb.SetComponent(farmerEntity, new Unity.Transforms.Translation() { Value = spawnPosition });
				ecb.AddComponent<AiTagFarmer>(farmerEntity);
				ecb.AddComponent<AiTagCommandIdle>(farmerEntity);
				ecb.AddComponent<AiTargetCell>(farmerEntity);

				newResources -= farmerCost.Value;
			}

			resources.Resources = newResources;
		}).Run();
		ecb.Playback(EntityManager);
		ecb.Dispose();

		m_Random = random;
	}
}

