namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories
{
	/*public class UnitCategoryViewBackend : UIBackend<CategoryViewData, UnitCategoryView>
	{
	}

	public class UnitCategoryView : UIPresentation<CategoryViewData>
	{
		[SerializeField]
		private Animator animator;
		
		protected override void OnDataUpdate(CategoryViewData data)
		{
			Backend.transform.SetParent(data.CategoryTransform, false);
			Backend.transform.SetAsFirstSibling();
		}

		public class DefaultRenderSystem : BaseRenderSystem<UnitCategoryView>
		{
			private bool triggerQuit;

			protected override void PrepareValues()
			{
				var inputSystem = EventSystem.current.GetComponent<StandaloneInputModule>();
				triggerQuit = Input.GetButton(inputSystem.cancelButton);
			}

			protected override void Render(UnitCategoryView definition)
			{
				if (triggerQuit && definition.Data.QuitView is { } quitAction)
					quitAction();
			}

			protected override void ClearValues()
			{
				triggerQuit = false;
			}
		}
	}*/
}