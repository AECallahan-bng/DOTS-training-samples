using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class MapGenerationSystem : SystemBase
{
    static internal int PosToIndex(int2 size, int2 pos)
    {
        int i = pos.y * size.x + pos.x;
        return i;
    }
    static internal int2 WorldToCell(float3 pos, float2 cellSize)
    {
        return new int2((int)math.floor(pos.x / cellSize.x), (int)math.floor(pos.z / cellSize.y));
    }
    static internal float3 CellToWorld(int2 cellPos, float2 cellSize)
    {
        return new float3(cellPos.x * cellSize.x, 0, cellPos.y * cellSize.y);
    }
    protected override void OnCreate()
    {
    }

    //internal static void DestroySectionCell(EntityCommandBuffer ecb,
    //    FarmContent content,
    //    GridSize size,
    //    DynamicBuffer<SectionWorldGrid> sectionGrid, 
    //    int2 pos)
    //{
    //    var posI = PosToIndex(new int2(size.Width, size.Height), pos);
    //    ecb.DestroyEntity(sectionGrid[posI].Value);
    //    sectionGrid[posI] = new SectionWorldGrid { Value = Entity.Null };
    //}

    //internal static void SetSectionCellRock(EntityCommandBuffer ecb,
    //    FarmContent content,
    //    GridSize size,
    //    DynamicBuffer<SectionWorldGrid> sectionGrid, 
    //    DynamicBuffer<SectionWorldCollision> sectionCollision, 
    //    int2 pos)
    //{
        

    //    var posI = PosToIndex(new int2(size.Width, size.Height), pos);
    //    ecb.DestroyEntity(sectionGrid[posI].Value);

    //    var cell = ecb.CreateEntity();
        
    //    EntityManager.AddComponent(cell, new RockHealth { Value = 10 });
    //    ecb.AddComponent(cell, new CellPosition { Value = pos });
    //    ecb.AddComponent(cell, new LocalToWorld() { Value = float4x4.identity });
    //    ecb.AddComponent(cell, new Translation() { Value = new float3(pos.x * content.CellSize.x, 0, pos.y * content.CellSize.y) });
    //    ecb.AddComponent(cell, new Rotation() { Value = quaternion.identity });
    //    ecb.AddBuffer<Child>(cell);

    //    var cellRock = ecb.Instantiate(content.Rock);
    //    ecb.AddComponent(cellRock, new Parent { Value = cell });
    //    ecb.AddComponent(cellRock, new LocalToParent { Value = float4x4.identity });
    //    ecb.AddComponent(cellRock, new LocalToWorld { Value = float4x4.identity });
    //    ecb.AppendToBuffer(cell, new Child() { Value = cellRock });

    //    sectionGrid[posI] = new SectionWorldGrid { Value = cell };
    //    sectionCollision[posI] = new SectionWorldCollision { Blocked = true };
    //}

    //internal static void SetSectionCellUntilledGround(SystemBase em, EntityCommandBuffer.ParallelWriter ecb, int sortKey,
    //    FarmContent content,
    //    GridSize size,
    //    DynamicBuffer<SectionWorldGrid> sectionGrid,
    //    DynamicBuffer<SectionWorldCollision> sectionCollision,
    //    int2 pos)
    //{
    //    var posI = PosToIndex(new int2(size.Width, size.Height), pos);
    //    // ecb.DestroyEntity(sortKey, sectionGrid[posI].Value);
    //    var cell = sectionGrid[posI].Value;
        
    //    if (em.HasComponent<RockHealth>(cell))
    //    {
    //        ecb.RemoveComponent<RockHealth>(sortKey, cell);
    //    }
        
        
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

    protected void GenerateEmpty(EntityCommandBuffer ecb)
    {

        var size = GetSingleton<GridSize>();
        var content = GetSingleton<FarmContent>();
        var mapEntity = ecb.CreateEntity();
		ecb.AddComponent<SectionWorldTag>(mapEntity);
        var map = ecb.AddBuffer<SectionWorldGrid>(mapEntity);
        var collision = ecb.AddBuffer<SectionWorldCollision>(mapEntity);
        
        var size2 = new int2(size.Width, size.Height);
        for (int y = 0; y != size.Height; ++y)
        {
            for (int x = 0; x != size.Width; ++x)
            {
                var cellPos = new int2(x, y);
                var cell = ecb.CreateEntity();
				if (content.GenerateTilled)
				{
					ecb.AddComponent<CellTagTilledGround>(cell);
				}
				else
				{
					ecb.AddComponent<CellTagUntilledGround>(cell);
				}
				ecb.AddComponent(cell, new CellPosition { Value = new int2(x,y) });
                ecb.AddComponent(cell, new Translation() { Value = CellToWorld( cellPos, content.CellSize) });
                ecb.AddComponent(cell, new Rotation() { Value = quaternion.identity });

                //ecb.AddComponent(cell, new LocalToWorld() { Value = float4x4.identity });
                //ecb.AddBuffer<Child>(cell);


                var ground = ecb.Instantiate(content.GenerateTilled ? content.TilledLand : content.UntilledLand);
                ecb.AddComponent(cell, new Translation() { Value = CellToWorld(cellPos, content.CellSize) });
                ecb.AddComponent(cell, new Rotation() { Value = quaternion.identity });
                //ecb.AddComponent(cellLand, new Parent { Value = cell });
                //ecb.AddComponent(cellLand, new LocalToParent { Value = float4x4.identity });
                //ecb.AddComponent(cellLand, new LocalToWorld { Value = float4x4.identity });
                ////ecb.AddComponent(cellLand, new Rotation() { Value = quaternion.identity });
                ////ecb.AddComponent(cellLand, new Translation() { Value = float3.zero });
                //ecb.AppendToBuffer(cell, new Child() { Value = cellLand });
                ecb.AddComponent(cell, new Ground() { Value = ground });
                ecb.AppendToBuffer(mapEntity, new SectionWorldGrid { Value = cell });
                ecb.AppendToBuffer(mapEntity, new SectionWorldCollision { Blocked = false });
            }
        }
    }
    void GenerateRocks(EntityCommandBuffer ecb)
    {
        
        var size = GetSingleton<GridSize>();
        var content = GetSingleton<FarmContent>();

        NativeArray<float> noise = new NativeArray<float>(size.Width * size.Height, Allocator.Temp);
        float offset = content.Seed % 10000;
        for (int y = 0; y != size.Height; ++y)
        {
            for (int x = 0; x != size.Width; ++x)
            {
                noise[y * size.Width + x] = Mathf.PerlinNoise(offset + x / (float)size.Height * 5, offset + y / (float)size.Width * 5) * 0.5f
                            + Mathf.PerlinNoise(offset + x / (float)size.Height * 20, offset + y / (float)size.Width * 20) * 0.5f;
            }
        }
        Entities.ForEach((DynamicBuffer<SectionWorldCollision> collision, DynamicBuffer<SectionWorldGrid> map) =>
        {
            var random = new Unity.Mathematics.Random(content.Seed);
            
            var size2 = new int2(size.Width, size.Height);


            for (int y = 0; y != size.Height; ++y)
            {
                for (int x = 0; x != size.Width; ++x)
                {
                    var cellPos = new int2(x, y);
                    var posI = PosToIndex(size2, cellPos);
                    if (noise[y * size.Width + x] < content.Rockthreshold && !collision[posI].Blocked)
                    {

                        var cell = map[posI].Value;

                        // retag cell
                        ecb.RemoveComponent<CellTagTeleporter>(cell);
                        ecb.RemoveComponent<CellTagTilledGround>(cell);
                        ecb.RemoveComponent<CellTagPlantedGround>(cell);
                        ecb.RemoveComponent<CellTagGrownCrop>(cell);
                        ecb.AddComponent(cell, new RockHealth { Value = 10 });

                        //// destroy children
                        //var children = GetBuffer<Child>(cell);
                        //for(int i =0; i != children.Length; ++i)
                        //{
                        //    ecb.SetComponent(children[i].Value, new Translation() { Value = new float3(-60, 0, 0) });
                        //    //ecb.RemoveComponent<Parent>(children[i].Value);
                        //    //ecb.RemoveComponent<LocalToParent>(children[i].Value);
                        //    //ecb.RemoveComponent<LocalToWorld>(children[i].Value);
                        //    //ecb.DestroyEntity(children[i].Value);
                        //}
                        //children.Clear();


                        var newOverE = ecb.Instantiate(content.Rock);
                        ecb.AddComponent(newOverE, new Translation() { Value = CellToWorld(cellPos, content.CellSize) });
                        ecb.AddComponent(newOverE, new Rotation() { Value = quaternion.identity });

                        if (HasComponent<Over>(cell))
                        {
                            var over = GetComponent<Over>(cell);
                            ecb.DestroyEntity(over.Value);
                            over.Value = newOverE;
                        } else
                        {
                            ecb.AddComponent(cell, new Over() { Value = newOverE });
                        }

                        //if (cell.Over != Entity.Null) ecb.DestroyEntity(cell.Over);

                        
                        //ecb.SetComponent(cell, new SectionWorldGrid() { Cell = cell.Cell, Ground = cell.Ground, Over = over });

                        //ecb.AddComponent(cellRock, new Parent { Value = cell });
                        //ecb.AddComponent(cellRock, new LocalToParent { Value = float4x4.identity });
                        //ecb.AddComponent(cellRock, new LocalToWorld { Value = float4x4.identity });
                        //ecb.AddComponent(cellRock, new Rotation() { Value = quaternion.identity });
                        //ecb.AddComponent(cellRock, new Translation() { Value = float3.zero });

                    }
                }
            }

        }).Run();

        noise.Dispose();
    }
    void GenerateTeleporters(EntityCommandBuffer ecb)
    {

        var size = GetSingleton<GridSize>();
        var content = GetSingleton<FarmContent>();
        Entities.ForEach((DynamicBuffer<SectionWorldCollision> collision, DynamicBuffer<SectionWorldGrid> map) =>
        {
            var random = new Unity.Mathematics.Random(1);
            var size2 = new int2(size.Width, size.Height);
            int max = 10000000;
            for (int i = 0; i != content.TeleporterCount && max > 0; --max)
            {
                var cellPos = random.NextInt2(size2);
                var posI = PosToIndex(size2, cellPos);
                if (!collision[posI].Blocked)
                {
                    var cell = map[posI].Value;
                    // retag cell
                    ecb.RemoveComponent<RockHealth>(cell);
                    ecb.RemoveComponent<CellTagTilledGround>(cell);
                    ecb.RemoveComponent<CellTagPlantedGround>(cell);
                    ecb.RemoveComponent<CellTagGrownCrop>(cell);
                    ecb.AddComponent< CellTagTeleporter>(cell);


                    var newOverE = ecb.Instantiate(content.Teleporter);
                    ecb.AddComponent(newOverE, new Translation() { Value = CellToWorld(cellPos, content.CellSize) });
                    ecb.AddComponent(newOverE, new Rotation() { Value = quaternion.identity });

                    if (HasComponent<Over>(cell))
                    {
                        var over = GetComponent<Over>(cell);
                        ecb.DestroyEntity(over.Value);
                        over.Value = newOverE;
                    }
                    else
                    {
                        ecb.AddComponent(cell, new Over() { Value = newOverE });
                    }


                    //// destroy children
                    //var children = GetBuffer<Child>(cell);
                    //for (int iC = 0; iC != children.Length; ++iC)
                    //{
                    //    ecb.SetComponent(children[iC].Value, new Translation() { Value = new float3(0, 0, -60) });
                    //    //ecb.SetComponent(children[iC].Value, new Parent());
                    //    //ecb.RemoveComponent<Parent>(children[iC].Value);
                    //    //ecb.RemoveComponent<LocalToParent>(children[iC].Value);
                    //    //ecb.DestroyEntity(children[iC].Value);
                    //}
                    //children.Clear();
                    //
                    //var cellTeleporter = ecb.Instantiate(content.Teleporter);
                    //ecb.AddComponent(cellTeleporter, new Parent { Value = cell });
                    //ecb.AddComponent(cellTeleporter, new LocalToParent { Value = float4x4.identity });
                    //ecb.AddComponent(cellTeleporter, new LocalToWorld { Value = float4x4.identity });
                    ////ecb.AddComponent(cellTeleporter, new Rotation() { Value = quaternion.identity });
                    ////ecb.AddComponent(cellTeleporter, new Translation() { Value = float3.zero });
                    //
                    ////var cellLand = ecb.Instantiate(content.UntilledLand);
                    ////ecb.AddComponent(cellLand, new Parent { Value = cell });
                    ////ecb.AddComponent(cellLand, new LocalToParent { Value = float4x4.identity });
                    ////ecb.AddComponent(cellLand, new LocalToWorld { Value = float4x4.identity });
                    //
                    ////ecb.DestroyEntity(map[posI].Value);
                    ////
                    ////var cell = ecb.CreateEntity();
                    ////ecb.AddComponent<CellTagTeleporter>(cell);
                    ////ecb.AddComponent(cell, new CellPosition { Value = pos });
                    ////ecb.AddComponent(cell, new LocalToWorld() { Value = float4x4.identity });
                    ////ecb.AddComponent(cell, new Translation() { Value = new float3(pos.x * content.CellSize.x, 0, pos.y * content.CellSize.y) });
                    ////ecb.AddComponent(cell, new Rotation() { Value = quaternion.identity });
                    ////ecb.AddBuffer<Child>(cell);
                    ////
                    ////var cellLand = ecb.Instantiate(content.UntilledLand);
                    ////ecb.AddComponent(cellLand, new Parent { Value = cell });
                    ////ecb.AddComponent(cellLand, new LocalToParent { Value = float4x4.identity });
                    ////ecb.AddComponent(cellLand, new LocalToWorld { Value = float4x4.identity });
                    ////ecb.AppendToBuffer(cell, new Child() { Value = cellLand });
                    ////
                    ////var cellTeleporter = ecb.Instantiate(content.Teleporter);
                    ////ecb.AddComponent(cellTeleporter, new Parent { Value = cell });
                    ////ecb.AddComponent(cellTeleporter, new LocalToParent { Value = float4x4.identity });
                    ////ecb.AddComponent(cellTeleporter, new LocalToWorld { Value = float4x4.identity });
                    ////ecb.AppendToBuffer(cell, new Child() { Value = cellTeleporter });
                    ////
                    ////
                    ////map[posI] = new SectionWorldGrid { Value = cell };
                    ////collision[posI] = new SectionWorldCollision { Blocked = true };
                    ++i;
                }
            }
        }).Run();
    }
    
    protected override void OnUpdate()
    {

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        GenerateEmpty(ecb);
        ecb.Playback(EntityManager);
        ecb.Dispose();

        ecb = new EntityCommandBuffer(Allocator.TempJob);
        GenerateTeleporters(ecb);
        ecb.Playback(EntityManager);
        ecb.Dispose();

        ecb = new EntityCommandBuffer(Allocator.TempJob);
        GenerateRocks(ecb);
        ecb.Playback(EntityManager);
        ecb.Dispose();



        this.Enabled = false;
    }
}
