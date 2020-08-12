using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public enum AiCommands
{
	Idle,
	Pick,
	Clear,
	Till,
	Plant,
	Relocate,
	Sell
}

[Serializable]
public struct AiCommandRequest : IComponentData
{
	public Entity RequestedAi;
	public int2 TargetPosition;
	public AiCommands CommandType;
}
