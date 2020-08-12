using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class AiCommandSell : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;

    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}
    protected override void OnUpdate()
    {
        var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();

		Entities
			.WithAll<AiTagCommandSell>()
			.ForEach((
				int entityInQueryIndex,
				ref Entity carrierEntity,
				ref AiCarriedObject carriedObjectComponent,
				in AiTargetCell targetCell,
				in Translation translationComponent) =>
			{
				int2 pos = new int2((int)translationComponent.Value.x, (int)translationComponent.Value.z);

				if (pos.Equals(targetCell.CellCoords))
				{
					ecb.AddComponent<CropSellingTag>(entityInQueryIndex, carriedObjectComponent.CarriedObjectEntity);
					ecb.RemoveComponent<AiCarriedObject>(entityInQueryIndex, carrierEntity);

					Entity transaction = ecb.CreateEntity(entityInQueryIndex);
					ecb.AddComponent(entityInQueryIndex, transaction, new SellTransaction() { Resources = 1, GridPosition = pos });

					ecb.RemoveComponent<AiTagCommandSell>(entityInQueryIndex, carrierEntity);
					ecb.AddComponent<AiTagCommandIdle>(entityInQueryIndex, carrierEntity);
				}
			}).ScheduleParallel();

		m_ECBSystem.AddJobHandleForProducer(Dependency);
    }
}
