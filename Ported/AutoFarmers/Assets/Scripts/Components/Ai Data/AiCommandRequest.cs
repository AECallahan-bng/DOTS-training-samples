using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

enum AiCommands
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
	Entity RequestedAi;
	int2 TargetPosition;
	AiCommands CommandType;
}
