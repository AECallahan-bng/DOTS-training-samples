using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class SpawnFarmersSystem : SystemBase
{
	protected override void OnUpdate()
	{
		var ecb = new EntityCommandBuffer(Allocator.TempJob);
		Entities.ForEach((ref FarmerResources resources, in FarmerCost farmerCost, in FarmContent farmContent) =>
		{
			int newResources = resources.Resources;

			while (newResources >= farmerCost.Value)
			{
				ecb.Instantiate(farmContent.Farmer);
				newResources -= farmerCost.Value;
			}

			resources.Resources = newResources;
		}).Run();
		ecb.Playback(EntityManager);
		ecb.Dispose();
	}
}

