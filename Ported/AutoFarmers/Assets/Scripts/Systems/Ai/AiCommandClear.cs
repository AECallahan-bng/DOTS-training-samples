using Unity.Collections;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class AiCommandClear : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;

    private EntityQuery m_RockQuery;
    protected override void OnCreate()
    {
        m_RockQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadWrite<RockHealth>()
            }
        });
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    //internal static void SetSectionCellUntilledGround(EntityCommandBuffer.ParallelWriter ecb, int sortKey,
    //    BufferFromEntity<Child> childAccessor,
    //    FarmContent content,
    //    GridSize size,
    //    DynamicBuffer<SectionWorldGrid> sectionGrid,
    //    //DynamicBuffer<SectionWorldCollision> sectionCollision,
    //    int2 pos)
    //{
    //    var posI = MapGenerationSystem.PosToIndex(new int2(size.Width, size.Height), pos);
    //    var cell = sectionGrid[posI].Value;
    //    //EntityManager.HasComponent

    //    ecb.RemoveComponent<RockHealth>(sortKey, cell);
    //    ecb.RemoveComponent<CellTagTeleporter>(sortKey, cell);
    //    ecb.RemoveComponent<CellTagTilledGround>(sortKey, cell);
    //    ecb.RemoveComponent<CellTagPlantedGround>(sortKey, cell);
    //    ecb.RemoveComponent<CellTagGrownCrop>(sortKey, cell);
    //    ecb.AddComponent<CellTagUntilledGround>(sortKey, cell);


    //    //delete all children
    //    var children = childAccessor[cell];
    //    foreach (var c in children)
    //    {
    //        ecb.DestroyEntity(sortKey, c.Value);
    //    }
    //    children.Clear();

    //    // Create Child land
    //    var cellLand = ecb.Instantiate(sortKey, content.UntilledLand);
    //    ecb.AddComponent(sortKey, cellLand, new Parent { Value = cell });
    //    ecb.AddComponent(sortKey, cellLand, new LocalToParent { Value = float4x4.identity });
    //    ecb.AddComponent(sortKey, cellLand, new LocalToWorld { Value = float4x4.identity });
    //    ecb.AppendToBuffer(sortKey, cell, new Child() { Value = cellLand });


    //    ecb.SetBuffer<Child>(sortKey, cell);


    //    //ecb.AddBuffer<Child>(sortKey, cell);
    //    ////var cell = ecb.CreateEntity(sortKey);
    //    //ecb.AddComponent<CellTagUntilledGround>(sortKey, cell);
    //    //ecb.AddComponent(sortKey, cell, new CellPosition { Value = pos });
    //    //ecb.AddComponent(sortKey, cell, new LocalToWorld() { Value = float4x4.identity });
    //    //ecb.AddComponent(sortKey, cell, new Translation() { Value = new float3(pos.x * content.CellSize.x, 0, pos.y * content.CellSize.y) });
    //    //ecb.AddComponent(sortKey, cell, new Rotation() { Value = quaternion.identity });
    //    //ecb.AddBuffer<Child>(sortKey, cell);
    //    //
    //    //var cellLand = ecb.Instantiate(sortKey, content.UntilledLand);
    //    //ecb.AddComponent(sortKey, cellLand, new Parent { Value = cell });
    //    //ecb.AddComponent(sortKey, cellLand, new LocalToParent { Value = float4x4.identity });
    //    //ecb.AddComponent(sortKey, cellLand, new LocalToWorld { Value = float4x4.identity });
    //    //ecb.AppendToBuffer(sortKey, cell, new Child() { Value = cellLand });
    //    ////ecb.
    //    //sectionGrid[posI] = new SectionWorldGrid { Value = cell };
    //    //sectionCollision[posI] = new SectionWorldCollision { Blocked = false };
    //}

    protected override void OnUpdate()
    {
        if (!HasSingleton<SectionWorldTag>()) return;
        var dt = Time.DeltaTime;
        var ecb = m_ECBSystem.CreateCommandBuffer().AsParallelWriter();
        var content = GetSingleton<FarmContent>();
        var gridSize = GetSingleton<GridSize>();
        var world = GetSingletonEntity<SectionWorldTag>();
        var map = GetBuffer<SectionWorldGrid>(world).AsNativeArray();
        var collision = GetBuffer<SectionWorldCollision>(world);
        var childAccessor = GetBufferFromEntity<Child>(true);
        //var rockHealths = GetArchetypeChunkComponentType<RockHealth>(false);
        //var rockHealths = GetComponentTypeHandle<RockHealth>(false);
        
        //var rockHealths = m_RockQuery.ToComponentDataArrayAsync<RockHealth>(Allocator.TempJob, out var rockHealthsHandle);
        //var rockHealthEntities = m_RockQuery.ToEntityArrayAsync(Allocator.TempJob, out var rockHealthEntitiesHandle);
        //
        //Dependency = JobHandle.CombineDependencies(Dependency, rockHealthsHandle);
        //Dependency = JobHandle.CombineDependencies(Dependency, rockHealthEntitiesHandle);

        Entities
            .WithReadOnly(map)
            //.WithReadOnly(childAccessor)
            .WithNativeDisableContainerSafetyRestriction(map)
            .WithNativeDisableContainerSafetyRestriction(childAccessor)
            //.WithReadOnly(collision)
            //.WithDisposeOnCompletion(rockHealths)
            //.WithDisposeOnCompletion(rockHealthEntities)
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
                    if(cmd.AnimationTime > 0.25f)
                    {
                        //hit the rock
                        cmd.AnimationTime = 0;
                        var cell = map[MapGenerationSystem.PosToIndex(curCellPosition, gridSize.Value)];
                        var rockE = cell.Value;
                        var rockHealth = GetComponent<RockHealth>(rockE);
                        //var rockEI = rockHealthEntities.IndexOf<Entity>(rockE);
                        //if(rockEI < 0)
                        //{
                        //    //there is no rock at that position..... wat???
                        //    ecb.RemoveComponent< AiTagCommandClear>(entityInQueryIndex, AiEntity);
                        //    ecb.AddComponent(entityInQueryIndex, AiEntity, new AiTagCommandIdle());
                        //    return;
                        //}
                        //
                        //int newHealth = rockHealths[rockEI].Value - 1;
                        int newHealth = rockHealth.Value - 1;
                        ecb.SetComponent(entityInQueryIndex, rockE, new RockHealth { Value = newHealth });
                        if (newHealth <= 0)
                        {
                            // rock broken, switch cell to untilled land
                            //SetSectionCellUntilledGround(ecb, entityInQueryIndex, childAccessor, content, gridSize, map, curCellPosition);


                            var posI = MapGenerationSystem.PosToIndex(gridSize.Value, curCellPosition);
                            var cellEntity = map[posI].Value;

                            //Retag cell
                            ecb.RemoveComponent<RockHealth>(entityInQueryIndex, cellEntity);
                            ecb.RemoveComponent<CellTagTeleporter>(entityInQueryIndex, cellEntity);
                            ecb.RemoveComponent<CellTagTilledGround>(entityInQueryIndex, cellEntity);
                            ecb.RemoveComponent<CellTagPlantedGround>(entityInQueryIndex, cellEntity);
                            ecb.RemoveComponent<CellTagGrownCrop>(entityInQueryIndex, cellEntity);
                            ecb.AddComponent<CellTagUntilledGround>(entityInQueryIndex, cellEntity);

                            // Destroy children
                            ////var children = ecb.SetBuffer<Child>(entityInQueryIndex, cell);
                            ////delete all children
                            var children = childAccessor[cellEntity]; //GetBuffer<Child>(cell);//childAccessor[cell];
                            //foreach (var c in children)
                            for(int i = 0; i != children.Length; ++i)
                            {
                                ecb.DestroyEntity(entityInQueryIndex, children[i].Value);
                            }
                            children.Clear();

                            // Create Child land
                            var cellLand = ecb.Instantiate(entityInQueryIndex, content.UntilledLand);
                            ecb.AddComponent(entityInQueryIndex, cellLand, new Parent { Value = cellEntity });
                            ecb.AddComponent(entityInQueryIndex, cellLand, new LocalToParent { Value = float4x4.identity });
                            ecb.AddComponent(entityInQueryIndex, cellLand, new LocalToWorld { Value = float4x4.identity });

                            // Ai is done
                            ecb.RemoveComponent<AiTagCommandClear>(entityInQueryIndex, AiEntity);
                            ecb.AddComponent(entityInQueryIndex, AiEntity, new AiTagCommandIdle());

                            //ecb.SetBuffer<Child>(entityInQueryIndex, cellEntity);
                            //ecb.AppendToBuffer(entityInQueryIndex, cellEntity, new Child() { Value = cellLand });
                        }
                        

                    }
                }
                else if (math.all(curCellPosition == targetCell.CellCoords))
                {
                    //we're at the rock, time to break it!
                    cmd.IsBreaking = true;
                    cmd.AnimationTime = 0;
                }
                //ecb.SetComponent(entityInQueryIndex, AiEntity, cmd);
                
                
            }).ScheduleParallel();
        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }

}
