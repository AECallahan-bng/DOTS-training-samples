using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class AiCommandPickSystem : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;
    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
	{
		UnityEngine.Debug.Log("Running Command Pick");

        if (HasSingleton<SectionWorldTag>())
        {
			var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();
			var size = GetSingleton<GridSize>();
			var worldGridEntity = GetSingletonEntity<SectionWorldTag>();
			var buffer = GetBuffer<SectionWorldGrid>(worldGridEntity);
			int2 sizeInt = new int2(size.Width, size.Height);

			FarmContent farmContent = GetSingleton<FarmContent>();

			Entities.WithAll<AiTagCommandPick>()
				.WithNativeDisableContainerSafetyRestriction(buffer)
				.ForEach((
				int entityInQueryIndex,
				ref Entity aiEntity,
				in AiTargetCell targetCell,
				in Translation translation) =>
			{
				int2 pos = new int2((int)translation.Value.x, (int)translation.Value.z);
				int bufferIndex = PosToIndex(sizeInt, targetCell.CellCoords);
				Entity entityInPos = buffer[bufferIndex].Value;

				if (pos.Equals(targetCell.CellCoords))
				{
					ecb.RemoveComponent<CellTagGrownCrop>(entityInQueryIndex, entityInPos);

					ecb.RemoveComponent<AiTagCommandPick>(entityInQueryIndex, aiEntity);
					ecb.AddComponent<AiTagCommandIdle>(entityInQueryIndex, aiEntity);
					ecb.AddComponent(entityInQueryIndex, aiEntity, new AiCarriedObject { CarriedObjectEntity = entityInPos });

					//buffer[bufferIndex] = new SectionWorldGrid { Value = ecb.Instantiate(entityInQueryIndex, farmContent.TilledLand) };
					//ecb.SetComponent(entityInQueryIndex, buffer[bufferIndex].Value, translation);
				}
			}).ScheduleParallel();
			m_ECBSystem.AddJobHandleForProducer(Dependency);
		}
    }

    static int PosToIndex(int2 size, int2 pos)
    {
        int i = pos.y * size.x + pos.x;
        return i;
    }
}
