using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class TeleportCropsSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float delta = Time.DeltaTime;
        
        Entities.WithAll<CropSellingTag>().ForEach((
            int entityInQueryIndex, 
            Translation translationComponent, 
            NonUniformScale scaleComponent ) => {
            
        }).ScheduleParallel();
    }
}
