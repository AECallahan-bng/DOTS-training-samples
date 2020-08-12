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

		if (HasSingleton<SectionWorldTag>())
		{
			var query = GetEntityQuery(typeof(SectionWorldTag));
			var buffer = GetBuffer<SectionWorldGrid>(query.GetSingletonEntity());
			var size = GetSingleton<GridSize>();
			int2 sizeInt = new int2(size.Width, size.Height);

			Entities
				.WithAll<AiTagCommandSell>()
				.WithNativeDisableParallelForRestriction(buffer)
				.ForEach((
				int entityInQueryIndex,
				ref Entity carrierEntity,
				ref AiCarriedObject carriedObjectComponent,
				in Translation translationComponent) =>
			{
				int2 pos = new int2((int)translationComponent.Value.x, (int)translationComponent.Value.z);

				Entity entityInPos = buffer[PosToIndex(sizeInt, pos)].Value;

				if (HasComponent<CellTagTeleporter>(entityInPos))
				{
					ecb.AddComponent<CropSellingTag>(entityInQueryIndex, carriedObjectComponent.CarriedObjectEntity);
					ecb.RemoveComponent<AiCarriedObject>(entityInQueryIndex, carrierEntity);

					Entity transaction = ecb.CreateEntity(entityInQueryIndex);
					ecb.AddComponent<SellTransaction>(entityInQueryIndex, transaction, new SellTransaction() { Resources = 1, GridPosition = pos });

					ecb.RemoveComponent<AiTagCommandSell>(entityInQueryIndex, carrierEntity);
					ecb.AddComponent<AiTagCommandIdle>(entityInQueryIndex, carrierEntity);
				}
			}).ScheduleParallel();
		}

        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }

    static int PosToIndex(int2 size, int2 pos)
    {
        int i = pos.y * size.x + pos.x;
        return i;
    }
}
