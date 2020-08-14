using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class CameraFollow : MonoBehaviour
{
	public Vector2 viewAngles;
	public float viewDist;
	public float minViewDist;
	public float maxViewDist;
	public float mouseSensitivity;
	public float scrollSensitivity;

	public bool OverviewCamera;

	void Start()
	{
		transform.rotation = Quaternion.Euler(viewAngles.y, viewAngles.x, 0f);
	}

	void LateUpdate()
	{
		float newViewDist = viewDist - scrollSensitivity * Input.mouseScrollDelta.y;

		viewDist = math.clamp(newViewDist, minViewDist, maxViewDist);

		if (Input.GetKeyDown(KeyCode.Space))
		{
			OverviewCamera = !OverviewCamera;
		}

		if (OverviewCamera)
		{
			int2 gridSize = World.DefaultGameObjectInjectionWorld.GetExistingSystem<MapGenerationSystem>().m_GridSize;

			transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
			transform.position = new Vector3(gridSize.x / 2, math.max(gridSize.x, gridSize.y), gridSize.y / 2);
		}
		else
		{
			Vector3 pos = World.DefaultGameObjectInjectionWorld.GetExistingSystem<SpawnFarmersSystem>().m_FirstFarmerPosition;

			viewAngles.x += Input.GetAxis("Mouse X") * mouseSensitivity / Screen.height;
			viewAngles.y -= Input.GetAxis("Mouse Y") * mouseSensitivity / Screen.height;
			viewAngles.y = Mathf.Clamp(viewAngles.y, 7f, 80f);
			viewAngles.x -= Mathf.Floor(viewAngles.x / 360f) * 360f;
			transform.rotation = Quaternion.Euler(viewAngles.y, viewAngles.x, 0f);
			transform.position = pos - transform.forward * viewDist;
		}
	}
}
