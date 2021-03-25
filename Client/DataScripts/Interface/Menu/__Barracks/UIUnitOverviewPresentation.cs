using PataNext.Client.Asset;
using PataNext.Client.Behaviors;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks
{
	public struct CurrentUnitOverview : IComponentData
	{
		public Entity Target;

		public CurrentUnitOverview(Entity target) => this.Target = target;
	}

	public struct UnitOverviewData
	{
	}

	public class UIUnitOverviewPresentation : UIPresentation<UnitOverviewData>
	{
		[SerializeField]
		private GameObject categoriesAsset;

		[SerializeField]
		private Transform categoriesRoot;

		public GameObject FocusCategoryGameObjectEffect;
		
		private IContainer<UIUnitOverviewCategoryRows> categoriesContainer;

		internal float TimeBeforeNextCategoryInput { get; set; }
		public   bool  IsCategoryFocused           { get; set; }

		public UIUnitOverviewCategoryRows Categories => categoriesContainer.GetList()[0];

		public override void OnBackendSet()
		{
			base.OnBackendSet();

			categoriesContainer = ContainerPool.FromPresentation<UIUnitOverviewCategoryRows.Backend, UIUnitOverviewCategoryRows>(categoriesAsset)
			                                   .WithTransformRoot(categoriesRoot);
			categoriesContainer.SetSize(1);
		}

		protected override void OnDataUpdate(UnitOverviewData data)
		{
		}

		public override void OnReset()
		{
			base.OnReset();
			
			categoriesContainer?.Dispose();
		}
	}

	public class UIUnitOverviewBackend : UIBackend<UnitOverviewData, UIUnitOverviewPresentation>
	{

	}

	public class UIUnitOverviewRenderSystem : BaseRenderSystem<UIUnitOverviewPresentation>
	{
		private int2  movInput;
		private bool2 inputUpdate;

		private bool enterInput;

		protected override void PrepareValues()
		{
			var inputSystem = EventSystem.current.GetComponent<InputSystemUIInputModule>();
			var moveInput   = inputSystem.move.action.ReadValue<Vector2>();

			var nextInput = new int2((int) math.sign(moveInput.x), (int) math.sign(moveInput.y));
			inputUpdate = movInput != nextInput;
			movInput    = nextInput;

			enterInput = inputSystem.submit.action.ReadValue<float>() > 0.1f;
		}

		protected override void Render(UIUnitOverviewPresentation definition)
		{
			definition.TimeBeforeNextCategoryInput -= Time.DeltaTime;
			if (false == definition.IsCategoryFocused)
			{
				if (movInput.y != 0 && (definition.TimeBeforeNextCategoryInput <= 0 || inputUpdate.y))
				{
					definition.TimeBeforeNextCategoryInput =  0.2f;
					definition.Categories.Cursor           += new int2(-movInput.y, 0);
				}

				if (definition.Categories.ItemRange.x > 0)
				{
					if (enterInput)
					{
						definition.IsCategoryFocused              = true;
						definition.Categories.CurrentlySelectedId = definition.Categories.Get(definition.Categories.Cursor).id;
					}
				}
			}

			if (definition.IsCategoryFocused && string.IsNullOrEmpty(definition.Categories.CurrentlySelectedId))
			{
				definition.IsCategoryFocused = false;
			}
			
			definition.FocusCategoryGameObjectEffect.SetActive(definition.IsCategoryFocused);
		}

		protected override void ClearValues()
		{

		}
	}
}