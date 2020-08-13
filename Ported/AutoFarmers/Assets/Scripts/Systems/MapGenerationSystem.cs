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
    static internal float3 RockOffset
    {
        get
        {
            return new float3(0, 0.5f, 0);
        }
    }
    static internal float3 TeleporterOffset
    {
        get
        {
            return new float3(0, 1.5f, 0);
        }
    }
    protected override void OnCreate()
    {
    }
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
                var ground = ecb.Instantiate(content.GenerateTilled ? content.TilledLand : content.UntilledLand);
                ecb.AddComponent(ground, new Translation() { Value = CellToWorld(cellPos, content.CellSize) });
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

                        var newOverE = ecb.Instantiate(content.Rock);
                        ecb.AddComponent(newOverE, new Translation() { Value = CellToWorld(cellPos, content.CellSize)+RockOffset});

                        if (HasComponent<Over>(cell))
                        {
                            var over = GetComponent<Over>(cell);
                            ecb.DestroyEntity(over.Value);
                            over.Value = newOverE;
                        } else
                        {
                            ecb.AddComponent(cell, new Over() { Value = newOverE });
                        }

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
                    ecb.AddComponent(newOverE, new Translation() { Value = CellToWorld(cellPos, content.CellSize) + TeleporterOffset });

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
