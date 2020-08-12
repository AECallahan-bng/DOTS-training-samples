using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class MapGenerationSystem : SystemBase
{
    protected override void OnCreate()
    {
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entities.ForEach((in GridSize size) =>
        {
            var map = ecb.CreateEntity();
            var data = ecb.AddBuffer<SectionWorldGrid>(map);
            for (int y = 0; y != size.Height; ++y)
            {
                for (int x = 0; x != size.Width; ++x)
                {
                    var cell = ecb.CreateEntity();
                    ecb.AppendToBuffer(map, new SectionWorldGrid { Value = cell });
                }
            }
        }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();

        this.Enabled = false;
    }
}

