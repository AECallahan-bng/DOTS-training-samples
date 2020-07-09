﻿using Unity.Entities;
using Unity.Mathematics;

// Fire simulation grid settings
[GenerateAuthoringComponent]
public struct FireGridSettings : IComponentData
{
    public uint2 FireGridResolution;
    public int MipDebugIndex;
}

public struct FireCell : IBufferElementData
{
    public float FireTemperature;
}

// TODO Bad
public struct FireCellHistory : IBufferElementData
{
    public float FireTemperaturePrev;
}

public struct FireCellFlag : IBufferElementData
{
    public bool OnFire;
}