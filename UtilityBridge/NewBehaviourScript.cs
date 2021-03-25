using System;
using UnityEngine.Jobs;

public static class InternalTransformUtility
{
	public static IntPtr GetSortedTransform(IntPtr array)
	{
		return TransformAccessArray.GetSortedTransformAccess(array);
	}
}
