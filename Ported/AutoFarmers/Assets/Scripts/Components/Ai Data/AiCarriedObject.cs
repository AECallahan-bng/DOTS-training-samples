using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct AiCarriedObject : IComponentData
{
	public Entity CarriedObjectEntity;
}
