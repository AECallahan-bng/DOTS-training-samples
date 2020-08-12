﻿using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Unity.Transforms;

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
        var mapEntity = ecb.CreateEntity();
		ecb.AddComponent<SectionWorldTag>(mapEntity);
        var map = ecb.AddBuffer<SectionWorldGrid>(mapEntity);
        var collision = ecb.AddBuffer<SectionWorldCollision>(mapEntity);
        
        var size2 = new int2(size.Width, size.Height);
        for (int y = 0; y != size.Height; ++y)
        {
            for (int x = 0; x != size.Width; ++x)
            {

                var cell = ecb.CreateEntity();
                ecb.AddComponent<CellTagUntilledGround>(cell);
                ecb.AddComponent(cell, new CellPosition { Value = new int2(x,y) });
                ecb.AddComponent(cell, new LocalToWorld() { Value = float4x4.identity });
                ecb.AddComponent(cell, new Translation() { Value = new float3(x * content.CellSize.x, 0, y * content.CellSize.y) });
                ecb.AddComponent(cell, new Rotation() { Value = quaternion.identity });
                ecb.AddBuffer<Child>(cell);

                var cellLand = ecb.Instantiate(content.UntilledLand);
                ecb.AddComponent(cellLand, new Parent { Value = cell });
                ecb.AddComponent(cellLand, new LocalToParent { Value = float4x4.identity });
                ecb.AddComponent(cellLand, new LocalToWorld { Value = float4x4.identity });
                ecb.AppendToBuffer(cell, new Child() { Value = cellLand });



                ecb.AppendToBuffer(mapEntity, new SectionWorldGrid { Value = cell });
                ecb.AppendToBuffer(mapEntity, new SectionWorldCollision { Blocked = false });

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
                    if (noise[x, y] > content.Rockthreshold && !collision[posI].Blocked)
                    {

                        ecb.DestroyEntity(map[posI].Value);

                        var cell = ecb.CreateEntity();
                        ecb.AddComponent(cell, new RockHealth { Value = 10 });
                        ecb.AddComponent(cell, new CellPosition { Value = pos });
                        ecb.AddComponent(cell, new Unity.Transforms.LocalToWorld() { Value = float4x4.identity });
                        ecb.AddComponent(cell, new Unity.Transforms.Translation() { Value = new float3(x * content.CellSize.x, 0, y * content.CellSize.y) });
                        ecb.AddComponent(cell, new Unity.Transforms.Rotation() { Value = quaternion.identity });
                        ecb.AddBuffer<Child>(cell);

                        var cellRock = ecb.Instantiate(content.Rock);
                        ecb.AddComponent(cellRock, new Unity.Transforms.Parent { Value = cell });
                        ecb.AddComponent(cellRock, new Unity.Transforms.LocalToParent { Value = float4x4.identity });
                        ecb.AddComponent(cellRock, new Unity.Transforms.LocalToWorld { Value = float4x4.identity });
                        ecb.AppendToBuffer(cell, new Child() { Value = cellRock });

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

                    var cell = ecb.CreateEntity();
                    ecb.AddComponent<CellTagTeleporter>(cell);
                    ecb.AddComponent(cell, new CellPosition { Value = pos });
                    ecb.AddComponent(cell, new LocalToWorld() { Value = float4x4.identity });
                    ecb.AddComponent(cell, new Translation() { Value = new float3(pos.x * content.CellSize.x, 0, pos.y * content.CellSize.y) });
                    ecb.AddComponent(cell, new Rotation() { Value = quaternion.identity });
                    ecb.AddBuffer<Child>(cell);

                    var cellLand = ecb.Instantiate(content.UntilledLand);
                    ecb.AddComponent(cellLand, new Parent { Value = cell });
                    ecb.AddComponent(cellLand, new LocalToParent { Value = float4x4.identity });
                    ecb.AddComponent(cellLand, new LocalToWorld { Value = float4x4.identity });
                    ecb.AppendToBuffer(cell, new Child() { Value = cellLand });

                    var cellTeleporter = ecb.Instantiate(content.Teleporter);
                    ecb.AddComponent(cellTeleporter, new Parent { Value = cell });
                    ecb.AddComponent(cellTeleporter, new LocalToParent { Value = float4x4.identity });
                    ecb.AddComponent(cellTeleporter, new LocalToWorld { Value = float4x4.identity });
                    ecb.AppendToBuffer(cell, new Child() { Value = cellTeleporter });


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

