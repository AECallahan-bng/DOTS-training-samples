using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class AiMoveSystem : SystemBase
{
	const float cMovementSpeed = 2.0f;

    protected override void OnUpdate()
    {
		float deltaTime = Time.DeltaTime;
        Entities.ForEach((ref Translation currentPosition, in AiTargetCell moveTarget) => {
			float2 direction = new float2(moveTarget.CellCoords.x + 0.5f, moveTarget.CellCoords.y + 0.5f) - new float2(currentPosition.Value.x, currentPosition.Value.z);

			if (math.abs(direction.x) > math.abs(direction.y))
			{
				currentPosition.Value.x += deltaTime * cMovementSpeed * math.sign(direction.x);
			}
			else
			{
				currentPosition.Value.z += deltaTime * cMovementSpeed * math.sign(direction.y);
			}

		}).ScheduleParallel();

		var getTranslation = GetComponentDataFromEntity<Translation>(true);
		Entities.ForEach((ref Translation currentPosition, in AiObjectBeingCarried beingCarried) =>
		{
			Translation carrierPosition = getTranslation[beingCarried.CarrierEntity];

			currentPosition.Value = carrierPosition.Value + math.up() * 0.8f;
		}).ScheduleParallel();

	}
}
