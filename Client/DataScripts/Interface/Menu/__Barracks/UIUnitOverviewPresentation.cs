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

	public class UIUnitOverviewPresentation : RuntimeAssetPresentation
	{
		public UIUnitOverviewCategoryRows categories;
		
		public bool                                       IsCategoryFocused { get; set; }
		
		internal float timeBeforeNextCategoryInput { get; set; }
	}

	public class UIUnitOverviewBackend : RuntimeAssetBackend<UIUnitOverviewPresentation>
	{

	}

	public class UIUnitOverviewRenderSystem : BaseRenderSystem<UIUnitOverviewPresentation>
	{
		private int2 movInput;
		private bool2 inputUpdate;

		private bool enterInput;

		protected override void PrepareValues()
		{
			var inputSystem = EventSystem.current.GetComponent<InputSystemUIInputModule>();
			var moveInput   = inputSystem.move.action.ReadValue<Vector2>();

			var nextInput = new int2((int) math.sign(moveInput.x), (int) math.sign(moveInput.y));
			inputUpdate   = this.movInput != nextInput;
			this.movInput = nextInput;

			this.enterInput = inputSystem.submit.action.ReadValue<float>() > 0.1f;
		}

		protected override void Render(UIUnitOverviewPresentation definition)
		{
			definition.timeBeforeNextCategoryInput -= Time.DeltaTime;
			if (movInput.y != 0 && (definition.timeBeforeNextCategoryInput <= 0 || inputUpdate.y))
			{
				definition.timeBeforeNextCategoryInput =  0.2f;
				definition.categories.Cursor += new int2(-movInput.y, 0);
			}

			if (definition.categories.ItemRange.x > 0)
			{
				if (enterInput)
				{
					definition.IsCategoryFocused              = true;
					definition.categories.CurrentlySelectedId = definition.categories.Get(definition.categories.Cursor).id;
				}
			}
		}

		protected override void ClearValues()
		{
			
		}
	}
}