using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Mathematics;
using Unity.Transforms;

public class AiCommandClear : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;

    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    internal void SetSectionCellUntilledGround(EntityCommandBuffer.ParallelWriter ecb, int sortKey,
        FarmContent content,
        GridSize size,
        DynamicBuffer<SectionWorldGrid> sectionGrid,
        DynamicBuffer<SectionWorldCollision> sectionCollision,
        int2 pos)
    {
        var posI = MapGenerationSystem.PosToIndex(new int2(size.Width, size.Height), pos);
        var cell = sectionGrid[posI].Value;

        if (HasComponent<RockHealth>(cell)) ecb.RemoveComponent<RockHealth>(sortKey, cell);
        if (HasComponent<CellTagTeleporter>(cell)) ecb.RemoveComponent<CellTagTeleporter>(sortKey, cell);
        if (HasComponent<CellTagTilledGround>(cell)) ecb.RemoveComponent<CellTagTilledGround>(sortKey, cell);
        if (HasComponent<CellTagPlantedGround>(cell)) ecb.RemoveComponent<CellTagPlantedGround>(sortKey, cell);
        if (HasComponent<CellTagGrownCrop>(cell)) ecb.RemoveComponent<CellTagGrownCrop>(sortKey, cell);

        //GetBuffer<>(cell)

        ecb.AddBuffer<Child>(sortKey, cell);
        ////var cell = ecb.CreateEntity(sortKey);
        //ecb.AddComponent<CellTagUntilledGround>(sortKey, cell);
        //ecb.AddComponent(sortKey, cell, new CellPosition { Value = pos });
        //ecb.AddComponent(sortKey, cell, new LocalToWorld() { Value = float4x4.identity });
        //ecb.AddComponent(sortKey, cell, new Translation() { Value = new float3(pos.x * content.CellSize.x, 0, pos.y * content.CellSize.y) });
        //ecb.AddComponent(sortKey, cell, new Rotation() { Value = quaternion.identity });
        //ecb.AddBuffer<Child>(sortKey, cell);
        //
        //var cellLand = ecb.Instantiate(sortKey, content.UntilledLand);
        //ecb.AddComponent(sortKey, cellLand, new Parent { Value = cell });
        //ecb.AddComponent(sortKey, cellLand, new LocalToParent { Value = float4x4.identity });
        //ecb.AddComponent(sortKey, cellLand, new LocalToWorld { Value = float4x4.identity });
        //ecb.AppendToBuffer(sortKey, cell, new Child() { Value = cellLand });
        ////ecb.
        //sectionGrid[posI] = new SectionWorldGrid { Value = cell };
        //sectionCollision[posI] = new SectionWorldCollision { Blocked = false };
    }

    protected override void OnUpdate()
    {
        if (!HasSingleton<SectionWorldTag>()) return;
        var dt = Time.DeltaTime;
        var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();
        var content = GetSingleton<FarmContent>();
        var gridSize = GetSingleton<GridSize>();
        var world = GetSingletonEntity<SectionWorldTag>();
        var map = GetBuffer<SectionWorldGrid>(world);//.AsNativeArray();
        //var collision = GetBuffer<SectionWorldCollision>(world);
        
        Entities
            .WithReadOnly(map)
            .WithNativeDisableContainerSafetyRestriction(map)
            //.WithReadOnly(collision)
            .ForEach((
            int entityInQueryIndex,
            ref Entity AiEntity,
            ref AiTagCommandClear cmd,
            in AiTargetCell targetCell,
            in Translation translationComponent) =>
            {
                var curCellPosition = MapGenerationSystem.WorldToCell(translationComponent.Value, content.CellSize);
                if (cmd.IsBreaking)
                {
                    cmd.AnimationTime += dt;
                    if(dt > 0.25f)
                    {
                        //hit the rock
                        
                        var rockE = map[MapGenerationSystem.PosToIndex(curCellPosition, gridSize.Value)].Value;
                        var health = GetComponent<RockHealth>(rockE);
                        --health.Value;
                        ecb.SetComponent(entityInQueryIndex, rockE, health);
                        if (health.Value < 0)
                        {
                            // rock broken, switch cell to untilled land
                            //MapGenerationSystem.SetSectionCellUntilledGround(this, ecb, entityInQueryIndex, content, gridSize, map, collision, curCellPosition);
                            
                        }
                        

                    }
                }
                else if (math.all(curCellPosition == targetCell.CellCoords))
                {
                    //we're at the rock, time to break it!
                    cmd.IsBreaking = true;
                    cmd.AnimationTime = 0;
                }
                
                
                
            }).ScheduleParallel();
        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }

}
