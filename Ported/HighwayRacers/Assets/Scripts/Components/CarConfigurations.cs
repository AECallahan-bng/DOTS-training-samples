﻿using Unity.Entities;

[GenerateAuthoringComponent]
public struct CarConfigurations : IComponentData
{
    public Entity CarPrefab;

    public float MinDefaultSpeed;
    public float MinOvertakeSpeed;
    public float MinDistanceToCarBeforeOvertaking;
    public float MinOvertakeEagerness;
    public float MinMergeSpace;

    public float MaxDefaultSpeed;
    public float MaxOvertakeSpeed;
    public float MaxDistanceToCarBeforeOvertaking;
    public float MaxOvertakeEagerness;
    public float MaxMergeSpace;

    public float Acceleration;
    public float Deceleration;
    public float MinDistanceToFront;
    public float DecollisionDeceleration;
}