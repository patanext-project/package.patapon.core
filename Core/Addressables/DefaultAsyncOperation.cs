using StormiumTeam.GameBase.Modules;
using Unity.Collections;
using UnityEngine;

namespace PataNext.Client.Core.Addressables
{
	public class DefaultAsyncOperation
	{
		public static AsyncOperationModule.HandleDataPair<TComponent, THandleData> InvokeExecute<TComponent, THandleData>(AsyncOperationModule module, ref int index)
			where THandleData : struct
		{
			var handleDataPair = module.Get<TComponent, THandleData>(index);
			if (handleDataPair == null || !handleDataPair.Handle.IsValid() || !handleDataPair.Handle.IsDone)
			{
				if (handleDataPair != null && handleDataPair.Handle.IsValid() == false)
				{
					Debug.LogError(handleDataPair.Handle.OperationException);
					Debug.LogError(handleDataPair.Handle.Status);
				}

				return new AsyncOperationModule.HandleDataPair<TComponent, THandleData>();
			}

			module.Handles.RemoveAtSwapBack(index);
			index--;

			return handleDataPair;
		}
	}
}