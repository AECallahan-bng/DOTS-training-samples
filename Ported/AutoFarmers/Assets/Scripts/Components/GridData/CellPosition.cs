﻿using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct CellPosition : IComponentData
{
	public int2 Value;
}
