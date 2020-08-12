﻿using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class SpawnDronesSystem : SystemBase
{
	protected override void OnUpdate()
	{
		var ecb = new EntityCommandBuffer(Allocator.TempJob);
		Entities.ForEach((ref DroneResources resources, in DroneCost droneCost, in FarmContent farmContent) =>
		{
			int newResources = resources.Resources;

			while (newResources >= droneCost.Cost)
			{
				for (int i = 0; i < droneCost.SpawnCount; i++)
				{
					ecb.Instantiate(farmContent.Drone);
				}
				newResources -= droneCost.Cost;
			}

			resources.Resources = newResources;
		}).Run();
		ecb.Playback(EntityManager);
		ecb.Dispose();
	}
}

