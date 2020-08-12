using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class CollectSalesSystem : SystemBase
{
	private EntityQuery m_SalesQuery;
	private EntityCommandBufferSystem m_ECBSystem;

	protected override void OnCreate()
	{
		m_SalesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<SellTransaction>()
			}
		});

		m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate()
	{
		var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();
		NativeArray<Entity> transactionEntities = m_SalesQuery.ToEntityArrayAsync(Allocator.TempJob, out var entitiesArrayHandle);
		NativeArray<SellTransaction> transactions = m_SalesQuery.ToComponentDataArrayAsync<SellTransaction>(Allocator.TempJob, out JobHandle transactionsArrayJobHandle);

		Dependency = JobHandle.CombineDependencies(Dependency, entitiesArrayHandle);
		Dependency = JobHandle.CombineDependencies(Dependency, transactionsArrayJobHandle);

		Entities
			.WithDisposeOnCompletion(transactionEntities)
			.WithDisposeOnCompletion(transactions)
			.ForEach((
				int entityInQueryIndex,
				ref Entity economyEntity,
				ref FarmerResources farmerResources, 
				ref DroneResources droneResources) =>
		{
			if (transactions.Length > 0)
			{
				int2 lastGridPosition = new int2(0, 0);
				int totalSales = 0;

				for (int i = 0; i < transactions.Length; i++)
				{
					totalSales += transactions[i].Resources;
					lastGridPosition = transactions[i].GridPosition;
					ecb.DestroyEntity(entityInQueryIndex, transactionEntities[i]);
				}

				farmerResources.Resources += totalSales;
				droneResources.Resources += totalSales;
				farmerResources.LastGridPosition = lastGridPosition;
				droneResources.LastGridPosition = lastGridPosition;
			}
		}).Schedule();

		m_ECBSystem.AddJobHandleForProducer(Dependency);
	}
}

