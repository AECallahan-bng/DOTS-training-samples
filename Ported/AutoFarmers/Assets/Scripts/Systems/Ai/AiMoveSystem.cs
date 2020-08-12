using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class AiMoveSystem : SystemBase
{
	const float cMovementSpeed = 1;

    protected override void OnUpdate()
    {
		float deltaTime = Time.DeltaTime;
        Entities.ForEach((ref Translation currentPosition, in AiTargetCell moveTarget) => {
			float2 direction = new float2(moveTarget.CellCoords.x, moveTarget.CellCoords.y) - new float2(currentPosition.Value.x, currentPosition.Value.z);

			if (direction.x > direction.y)
			{
				currentPosition.Value.x += deltaTime * cMovementSpeed;
			}
			else
			{
				currentPosition.Value.z += deltaTime * cMovementSpeed;
			}

		}).ScheduleParallel();
    }
}
