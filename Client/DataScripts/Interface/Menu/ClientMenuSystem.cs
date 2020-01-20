using System;
using Karambolo.Common;
using package.patapon.core.Animation;
using Patapon.Client.PoolingSystems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace DataScripts.Interface.Menu
{
	public struct MenuData : IComponentData
	{
	}

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class ClientMenuSystem : ComponentSystem
	{
		public Type            PreviousMenu     => CurrentAnimation.PreviousType;
		public Type            CurrentMenu      => CurrentAnimation.Type;
		public TargetAnimation CurrentAnimation { get; private set; }

		public event Action<TargetAnimation> OnMenuUpdate;

		private Canvas m_Canvas;
		private Image  m_QuadBackground;

		protected override void OnCreate()
		{
			base.OnCreate();
			EntityManager.CreateEntity(typeof(MenuData));

			m_Canvas = CanvasUtility.Create(World, -1, "Background Canvas");
			var bgGo = new GameObject("BackgroundQuad", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			bgGo.transform.SetParent(m_Canvas.transform, false);

			m_QuadBackground = bgGo.GetComponent<Image>();
			CanvasUtility.ExtendRectTransform(m_QuadBackground.GetComponent<RectTransform>());

			SetBackgroundCanvasColor(Color.black);
		}

		protected override void OnUpdate()
		{

		}

		public void SetBackgroundCanvasColor(Color color)
		{
			m_QuadBackground.color = color;
		}

		public void SetDefaultMenu()
		{
			SetMenu(null);
		}

		public void SetMenu(Type type)
		{
			CurrentAnimation = new TargetAnimation(type, previousType: CurrentMenu);
			Debug.Log($"SetMenu, curr={type}, prev={CurrentAnimation.PreviousType}");
			
			if (PreviousMenu != null
			    && PreviousMenu.HasInterface(typeof(IMenuCallbacks))
			    && PreviousMenu.IsSubclassOf(typeof(ComponentSystemBase)))
			{
				var system = (IMenuCallbacks) World.GetExistingSystem(PreviousMenu);
				system.OnMenuUnset(CurrentAnimation);
			}

			if (type != null
			    && type.HasInterface(typeof(IMenuCallbacks))
			    && type.IsSubclassOf(typeof(ComponentSystemBase)))
			{
				var system = (IMenuCallbacks) World.GetExistingSystem(type);
				system.OnMenuSet(CurrentAnimation);
			}

			OnMenuUpdate?.Invoke(CurrentAnimation);
		}

		public void SetMenu<T>()
			where T : IMenu, new()
		{
			SetMenu(typeof(T));
		}

		public void SetManual(TargetAnimation target)
		{
			CurrentAnimation = target;
			OnMenuUpdate?.Invoke(CurrentAnimation);
		}

		public bool IsMenuActive<T>()
			where T : IMenu
		{
			return CurrentMenu == typeof(T);
		}
	}

	public interface IMenu
	{
	}

	public interface IMenuCallbacks
	{
		void OnMenuSet(TargetAnimation   current);
		void OnMenuUnset(TargetAnimation current);
	}
}