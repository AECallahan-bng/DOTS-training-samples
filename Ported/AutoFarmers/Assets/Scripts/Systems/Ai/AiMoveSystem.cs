using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class AiMoveSystem : SystemBase
{
	const float cFarmerMovementSpeed = 4.0f;
	const float cDroneMovementSpeed = 6.0f;
	const float cDroneVerticalSpeed = 2.0f;

	protected override void OnUpdate()
    {
		float deltaTime = Time.DeltaTime;
		float farmerSmooth = 1f - math.pow(0.003f, deltaTime);
		float droneSmooth = 1f - math.pow(0.1f, deltaTime);

		Entities.WithNone<AiTagCommandIdle>().ForEach((
			int entityInQueryIndex,
			Entity aiEntity,
			ref MovePosition currentPosition,
			ref Translation smoothPosition, 
			ref Rotation currentRotation,
			in AiTargetCell moveTarget) =>
		{
			bool IsFarmer = HasComponent<AiTagFarmer>(aiEntity);        // PERF: Could test this on a per-chunk basis if we use IJobChunk
			float movementSpeed = IsFarmer ? cFarmerMovementSpeed : cDroneMovementSpeed;
			float3 destination = new float3(moveTarget.CellCoords.x + 0.5f, 0.0f, moveTarget.CellCoords.y + 0.5f);
			float3 direction = destination - currentPosition.Value;

			if (IsFarmer)
			{
				float deltaPosition = deltaTime * movementSpeed;

				// farmers travel along manhattan coordinates and diagonals
				// if (math.abs(direction.x) > math.abs(direction.z))
				{
					if (direction.x > deltaPosition)
					{
						currentPosition.Value.x += deltaPosition;
					}
					else if (direction.x < -deltaPosition)
					{
						currentPosition.Value.x -= deltaPosition;
					}
					else
					{
						currentPosition.Value.x = destination.x;
					}
				}

				// else

				{
					if (direction.z > deltaPosition)
					{
						currentPosition.Value.z += deltaPosition;
					}
					else if (direction.z < -deltaPosition)
					{
						currentPosition.Value.z -= deltaPosition;
					}
					else
					{
						currentPosition.Value.z = destination.z;
					}
				}
			}
			else
			{
				float direction2d_sq = direction.x * direction.x + direction.z * direction.z;
				float maxMove = deltaTime * cDroneMovementSpeed;

				if (direction2d_sq < maxMove * maxMove)
				{
					currentPosition.Value.x += direction.x;
					currentPosition.Value.z += direction.z;
				}
				else
				{
					float direction2d_sqrt = math.sqrt(direction2d_sq);

					currentPosition.Value.x += maxMove * direction.x / direction2d_sqrt;
					currentPosition.Value.z += maxMove * direction.z / direction2d_sqrt;

					// drones travel at Y = 6 until near their destination
					if (direction2d_sqrt > 3.0f)
					{
						destination.y = 6.0f;
						direction.y = destination.y - currentPosition.Value.y;
					}
				}

				currentPosition.Value.y += deltaTime * cDroneVerticalSpeed * math.sign(direction.y);
			}

			// smoothPosition.Value = currentPosition.Value;
			smoothPosition.Value = math.lerp(smoothPosition.Value, currentPosition.Value, IsFarmer ? farmerSmooth : droneSmooth);

			if (!IsFarmer)
			{
				float3 tilt = new float3(currentPosition.Value.x - smoothPosition.Value.x, 2.0f, currentPosition.Value.z - smoothPosition.Value.z);
				float3 forward = math.normalize(math.cross(tilt, math.up()));

				currentRotation.Value = quaternion.LookRotationSafe(forward, tilt);
			}

		}).ScheduleParallel();

		// NOTE: this writes to Translation for carried objects (crops) and reads from Translation for carrier objects (farmers/drones)
		// which is why we need the WithNativeDisableContainerSafetyRestriction
		var getTranslation = GetComponentDataFromEntity<Translation>(true);
		Entities
			.WithReadOnly(getTranslation)
			.WithNativeDisableContainerSafetyRestriction(getTranslation)
			.ForEach((ref Translation currentPosition, in AiObjectBeingCarried beingCarried) =>
		{
			Translation carrierPosition = getTranslation[beingCarried.CarrierEntity];

			currentPosition.Value = carrierPosition.Value + math.up() * 0.8f;
		}).ScheduleParallel();

	}
}
