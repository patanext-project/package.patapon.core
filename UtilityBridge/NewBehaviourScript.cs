using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;

public static class InternalTransformUtility
{
	public static IntPtr GetSortedTransform(IntPtr array)
	{
		return TransformAccessArray.GetSortedTransformAccess(array);
	}
}
