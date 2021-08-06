using System;
using System.Collections.Generic;
using StormiumTeam.GameBase.Utility.AssetBackend;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PataNext.Client.Asset
{
	public abstract class UIPresentation<TData> : RuntimeAssetPresentation
	{
		protected internal abstract void OnDataUpdate(TData data);

		public override void OnBackendSet()
		{
			if (Backend is IDataStorage<TData> dataStorage)
				OnDataUpdate(dataStorage.Data);

			base.OnBackendSet();
		}

		public TData Data
		{
			get => ((IDataStorage<TData>) Backend).Data;
			set => ((IDataStorage<TData>) Backend).Data = value;
		}
		
		private List<(Action remove, Delegate action)> removeOnReset = new List<(Action remove, Delegate action)>();

		// Attach(button.onClick.AddListener, () => {});
		protected void Attach(Action<UnityAction> origin, Action<UnityAction> remove, UnityAction unityAction)
		{
			origin(unityAction);
			removeOnReset.Add((() => remove(unityAction), unityAction));
		}
		
		protected void Attach<T>(Action<UnityAction<T>> origin, Action<UnityAction<T>> remove, UnityAction<T> unityAction)
		{
			origin(unityAction);
			removeOnReset.Add((() => remove(unityAction), unityAction));
		}

		protected void Attach(UnityEvent ev, UnityAction action)
		{
			Attach(ev.AddListener, ev.RemoveListener, action);
		}
		
		protected void Attach<T>(UnityEvent<T> ev, UnityAction<T> action)
		{
			Attach(ev.AddListener, ev.RemoveListener, action);
		}

		protected void OnClick(Button button, UnityAction action)
		{
			Attach(button.onClick.AddListener, button.onClick.RemoveListener, action);
		}

		public override void OnReset()
		{
			base.OnReset();

			foreach (var item in removeOnReset)
				item.remove();
			removeOnReset.Clear();
		}
	}

	public class UIBackend<TData, TPresentation> : RuntimeAssetBackend<TPresentation>, IDataStorage<TData>
		where TPresentation : UIPresentation<TData>
	{
		// UI need to be rescaled
		public override bool PresentationWorldTransformStayOnSpawn => false;

		private TData data;

		public virtual TData Data
		{
			get => data;
			set
			{
				data = value;
				if (Presentation != null)
				{
					Presentation.OnDataUpdate(data);
				}
			}
		}
	}

	public interface IDataStorage<TData>
	{
		public TData Data { get; set; }
	}
}