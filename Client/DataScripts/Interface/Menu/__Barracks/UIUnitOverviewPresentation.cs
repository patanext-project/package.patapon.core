using System.Collections.Generic;
using PataNext.Client.Asset;
using PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks
{
	public struct CurrentUnitOverview : IComponentData
	{
		public Entity Target;

		public CurrentUnitOverview(Entity target) => this.Target = target;
		
		public struct RequestToQuit : IComponentData {}
	}

	public struct UnitOverviewData
	{
		public Entity TargetUnitEntity;
	}

	public class UIUnitOverviewPresentation : UIPresentation<UnitOverviewData>
	{
		[SerializeField]
		public Transform categoriesRoot;

		public Transform hideRoot;
		
		internal float TimeBeforeNextCategoryInput { get; set; }
		internal int   FocusedIndex                { get; set; }
		
		private UIOverviewModuleBase focused, active;
		
		internal double                  inputFrame;

		public UIOverviewModuleBase Focused
		{
			get => focused;
			set
			{
				if (focused == value)
					return;
				
				if (focused != null)
					focused.SetSelected(false);
				focused = value;

				if (focused != null)
					focused.SetSelected(true);
			}
		}

		public UIOverviewModuleBase Active
		{
			get => active;
			set
			{
				if (active != null)
				{
					active.ForceExit();
					active.IsActive = false;
				}

				active = null;
				if (value != null && value.Enter())
				{
					active          = value;
					active.IsActive = true;
				}
			}
		}

		protected override void OnDataUpdate(UnitOverviewData data)
		{
			foreach (var module in Categories)
			{
				var md = module.Data;
				md.Entity   = data.TargetUnitEntity;
				module.Data = md;
			}
		}

		public override void OnReset()
		{
			Active  = null;
			Focused = null;

			FocusedIndex = 0;
			
			if (backendPool != null)
				backendPool.Dispose();

			Categories.Clear();

			base.OnReset();
		}

		internal List<UIOverviewModuleBase> Categories = new List<UIOverviewModuleBase>();
		private  AssetPool<GameObject>      backendPool;

		public override void OnBackendSet()
		{
			base.OnBackendSet();

			var reference = new GameObject();
			backendPool = new AssetPool<GameObject>(pool =>
			{
				var go = new GameObject($"pooled module backend");
				go.SetActive(false);

				go.AddComponent<UIOverviewModuleBackend>();
				go.AddComponent<RectTransform>();
				go.AddComponent<GameObjectEntity>();
				return go;
			});

			var root = "Interface/Menu/__Barracks";
			AddModule(("st.pn", $"{root}/Controls/UnitStatistics/Prefab"));
			AddModule(("st.pn", $"{root}/Controls/UnitEquipment/UnitEquipment"));
			AddModule(("st.pn", $"{root}/Controls/UnitPresets/UnitPreset"));

			inputFrame = Time.time + 0.15f;
		}

		public void AddModule(UIOverviewModuleBase module)
		{
			module.Data = new ModuleViewData()
			{
				CategoryTransform = categoriesRoot,
				RootTransform     = transform,
				QuitView = () =>
				{
					active.IsActive = false;
					active          = null;

					inputFrame = Time.time + 0.1f;
				},

				Entity = Data.TargetUnitEntity
			};

			Categories.Add(module);
		}

		public void AddModule(AssetPath path)
		{
			var go = AssetManager.LoadAsset<GameObject>(path);
			if (go == null)
			{
				Debug.LogWarning("No asset found for " + path.ToString());
				return;
			}

			go = Instantiate(go);

			var goBackend = backendPool.Dequeue();
			goBackend.SetActive(true);
				
			var backend = goBackend.GetComponent<UIOverviewModuleBackend>();
			backend.OnReset();
			backend.SetTarget(Backend.DstEntityManager);
			backend.SetPresentationSingle(go);
			
			AddModule(go.GetComponent<UIOverviewModuleBase>());
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
		private bool quitInput;

		protected override void PrepareValues()
		{
			var inputSystem = EventSystem.current.GetComponent<StandaloneInputModule>();
			var  moveInput   = new Vector2(Input.GetAxis(inputSystem.horizontalAxis), Input.GetAxis(inputSystem.verticalAxis));

			var nextInput = new int2((int) math.sign(moveInput.x), (int) math.sign(moveInput.y));
			inputUpdate = movInput != nextInput;
			movInput    = nextInput;

			enterInput = Input.GetButtonDown(inputSystem.submitButton);
			quitInput = Input.GetButtonDown(inputSystem.cancelButton);
		}

		protected override void Render(UIUnitOverviewPresentation definition)
		{
			definition.TimeBeforeNextCategoryInput -= Time.DeltaTime;
			if (movInput.y != 0 && (definition.TimeBeforeNextCategoryInput <= 0 || inputUpdate.y))
			{
				definition.TimeBeforeNextCategoryInput =  0.2f;
				definition.FocusedIndex                += -movInput.y;
			}

			if (definition.Focused != null && definition.Active == null && enterInput && definition.inputFrame < Time.ElapsedTime)
			{
				definition.Active = definition.Focused;
			}

			if (definition.Active == null && quitInput && definition.inputFrame < Time.ElapsedTime)
			{
				Debug.LogError("quit");
				EntityManager.AddComponent<CurrentUnitOverview.RequestToQuit>(definition.Backend.DstEntity);
			}

			definition.FocusedIndex = Mathf.Clamp(definition.FocusedIndex, 0, definition.Categories.Count - 1);
			definition.Focused      = definition.Categories[definition.FocusedIndex];

			definition.categoriesRoot.localScale = Vector3.one;
			if (definition.Active != null)
			{
				definition.Focused = null;
				if (definition.Active.OnActiveHideUnitModule)
				{
					definition.categoriesRoot.localScale = Vector3.zero;
				}
			}
		}

		protected override void ClearValues()
		{

		}
	}
}