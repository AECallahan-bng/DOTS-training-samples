using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class SpawnFarmersSystem : SystemBase
{
	private Random m_Random;
	public Entity m_FirstFarmer;
	public float3 m_FirstFarmerPosition;
	public int CurrentCount=0;

	protected override void OnCreate()
	{
		m_Random = new Random(0x1234567);
		m_FirstFarmer = Entity.Null;
	}

	protected override void OnUpdate()
	{
		var ecb = new EntityCommandBuffer(Allocator.TempJob);
		var random = m_Random;
        var createdCount = 0;

        Entities.WithName("Spawn_Farmers").ForEach((ref FarmerResources resources, in FarmerCost farmerCost, in FarmContent farmContent) =>
		{
			int newResources = resources.Resources;

			while (newResources >= farmerCost.Value)
			{
				float3 spawnPosition = new float3(resources.LastGridPosition.x + 0.5f, 0, resources.LastGridPosition.y + 0.5f);

				var farmerEntity = ecb.Instantiate(farmContent.Farmer);
				ecb.SetComponent(farmerEntity, new Unity.Transforms.Translation() { Value = spawnPosition });
				ecb.AddComponent(farmerEntity, new MovePosition() { Value = spawnPosition });
				ecb.AddComponent<AiTagFarmer>(farmerEntity);
				ecb.AddComponent<AiTagCommandIdle>(farmerEntity);
				ecb.AddComponent<AiTargetCell>(farmerEntity);

				newResources -= farmerCost.Value;
				++createdCount;
			}

			resources.Resources = newResources;
		}).Run();
		ecb.Playback(EntityManager);
		ecb.Dispose();
        CurrentCount += createdCount;

		m_Random = random;

		if (m_FirstFarmer == Entity.Null)
		{
			var firstFarmer = m_FirstFarmer;

			Entities.WithName("FindFirstFarmer").ForEach((
				int entityInQueryIndex,
				Entity farmerEntity,
				in AiTagFarmer farmer) =>
			{
				if (firstFarmer == Entity.Null)
				{
					firstFarmer = farmerEntity;
				}
			}).Run();

			m_FirstFarmer = firstFarmer;
		}

		if (m_FirstFarmer != Entity.Null)
		{
			var getTranslation = GetComponentDataFromEntity<Translation>(true);

			m_FirstFarmerPosition = getTranslation[m_FirstFarmer].Value;
		}
	}
}

