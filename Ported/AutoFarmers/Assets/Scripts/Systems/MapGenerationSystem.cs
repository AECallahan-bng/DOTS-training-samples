using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class MapGenerationSystem : SystemBase
{
    static int PosToIndex(int2 size, int2 pos)
    {
        int i = pos.y * size.x + pos.x;
        return i;
    }
    protected override void OnCreate()
    {
    }

    

    protected void GenerateEmpty(EntityCommandBuffer ecb)
    {

        var size = GetSingleton<GridSize>();
        var content = GetSingleton<FarmContent>();
        var map = ecb.CreateEntity();
        var dataEntities = ecb.AddBuffer<SectionWorldGrid>(map);
        var dataCollisions = ecb.AddBuffer<SectionWorldCollision>(map);

        for (int y = 0; y != size.Height; ++y)
        {
            for (int x = 0; x != size.Width; ++x)
            {
                var cell = ecb.Instantiate(content.UntilledLand);
                ecb.SetComponent(cell, new Unity.Transforms.Translation() { Value = new float3(x * content.CellSize.x, 0, y * content.CellSize.y) });
                ecb.AddComponent<CellTagUntilledGround>(cell);
                ecb.AppendToBuffer(map, new SectionWorldGrid { Value = cell });
                ecb.AppendToBuffer(map, new SectionWorldCollision { Blocked = false });
            }
        }
    }
    void GenerateRocks(EntityCommandBuffer ecb)
    {
        var size = GetSingleton<GridSize>();
        var content = GetSingleton<FarmContent>();
        
        float[,] noise = new float[size.Width, size.Height];
        float offset = content.Seed % 10000;
        for (int y = 0; y != size.Height; ++y)
        {
            for (int x = 0; x != size.Width; ++x)
            {
                noise[x, y] = Mathf.PerlinNoise(offset + x / (float)size.Height * 5, offset + y / (float)size.Width * 5) * 0.5f
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
                    var pos = new int2(x, y);
                    var posI = PosToIndex(size2, pos);
                    if (noise[x, y] > content.Rockthreshold)
                    {
                        ecb.DestroyEntity(map[posI].Value);
                        var cell = ecb.Instantiate(content.Rock);
                        ecb.SetComponent(cell, new Unity.Transforms.Translation() { Value = new float3(pos.x * content.CellSize.x, 0, pos.y * content.CellSize.y) });
                        ecb.AddComponent(cell, new RockHealth { Value = 10 });
                        map[posI] = new SectionWorldGrid { Value = cell };
                        collision[posI] = new SectionWorldCollision { Blocked = true };

                    }
                }
            }

        }).Run();
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
                var pos = random.NextInt2(size2);
                var posI = PosToIndex(size2, pos);
                if (!collision[posI].Blocked)
                {
                    ecb.DestroyEntity(map[posI].Value);
                    var cell = ecb.Instantiate(content.Teleporter);
                    ecb.SetComponent(cell, new Unity.Transforms.Translation() { Value = new float3(pos.x * content.CellSize.x, 0, pos.y * content.CellSize.y) });
                    ecb.AddComponent<CellTagTeleporter>(cell);
                    map[posI] = new SectionWorldGrid { Value = cell };
                    collision[posI] = new SectionWorldCollision { Blocked = true };
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
        GenerateRocks(ecb);
        ecb.Playback(EntityManager);
        ecb.Dispose();
        

        ecb = new EntityCommandBuffer(Allocator.TempJob);
        GenerateTeleporters(ecb);
        ecb.Playback(EntityManager);
        ecb.Dispose();

        this.Enabled = false;
    }
}

