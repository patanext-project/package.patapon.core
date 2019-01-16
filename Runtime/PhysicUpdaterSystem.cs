using package.stormiumteam.shared;
using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace package.patapon.core
{
    public enum PhysicUpdateMode
    {
        TimeSettings,
        Framerate,
        FrameCustom,
        Custom
    }

    public interface IPhysicPreSimulate : IAppEvent
    {
        void PreSimulate();
    }

    public interface IPhysicPreSimulateItem : IAppEvent
    {
        void PreSimulateItem(float dt);
    }

    public interface IPhysicPostSimulateItem : IAppEvent
    {
        void PostSimulateItem(float dt);
    }

    public interface IPhysicPostSimulate : IAppEvent
    {
        void PostSimulate();
    }

    [UpdateAfter(typeof(Update))]
    [AlwaysUpdateSystem]
    public class PhysicUpdaterSystem : ComponentSystem
    {
        private          float          m_Timer;
        [Inject] private AppEventSystem m_AppEventSystem;

        public int   LastIterationCount;
        public float LastFixedTimeStep;

        public float CustomFixedTimeStep = 0.02f;
        public int   CustomIterationCount;

        public PhysicUpdateMode UpdateMode;

        protected override void OnCreateManager()
        {
            UpdateMode = PhysicUpdateMode.FrameCustom;
        }

        protected override void OnUpdate()
        {
            m_Timer += Time.deltaTime;

            foreach (var manager in AppEvent<IPhysicPreSimulate>.GetObjEvents())
            {
                manager.PreSimulate();
            }

            var delta = 0f;

            LastIterationCount = 0;
            LastFixedTimeStep  = 0f;

            if (UpdateMode == PhysicUpdateMode.FrameCustom
                && Application.targetFrameRate <= 0
                && QualitySettings.vSyncCount == 0)
            {
                Debug.LogWarning("PhysicUpdaterSystem: Switching update mode to Framerate\nCause: TargetFrameRate is at 0 and Vsync is disabled");
                UpdateMode = PhysicUpdateMode.Framerate;
            }

            var currentMode = UpdateMode;
            if (currentMode == PhysicUpdateMode.TimeSettings)
            {
                delta = Time.fixedDeltaTime;

                while (m_Timer >= delta)
                {
                    m_Timer           -= delta;
                    LastFixedTimeStep =  delta;
                    LastIterationCount++;
                }
            }
            else
            {
                m_Timer = 0f;

                switch (currentMode)
                {
                    case PhysicUpdateMode.Framerate:
                    {
                        delta = Time.deltaTime;

                        LastFixedTimeStep  = delta;
                        LastIterationCount = 1;
                        break;
                    }
                    case PhysicUpdateMode.FrameCustom:
                    {
                        var frameRate = Application.targetFrameRate;
                        if (QualitySettings.vSyncCount == 1)
                            frameRate = 60;
                        else if (QualitySettings.vSyncCount == 2)
                            frameRate = 30;

                        if (frameRate == 0)
                        {
                            Debug.LogWarning("FrameCustom mode returned a 0 framerate");
                        }

                        delta = 1f / frameRate;

                        LastFixedTimeStep  = delta;
                        LastIterationCount = Mathf.Max(1, CustomIterationCount);
                        break;
                    }
                    case PhysicUpdateMode.Custom:
                    {
                        LastFixedTimeStep  = CustomFixedTimeStep;
                        LastIterationCount = CustomIterationCount;
                        break;
                    }
                    case PhysicUpdateMode.TimeSettings:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            for (int i = 0; i != LastIterationCount; i++)
            {
                foreach (var manager in AppEvent<IPhysicPreSimulateItem>.GetObjEvents())
                {
                    manager.PreSimulateItem(delta);
                }

                Physics.Simulate(delta);

                foreach (var manager in AppEvent<IPhysicPostSimulateItem>.GetObjEvents())
                {
                    manager.PostSimulateItem(delta);
                }
            }

            Physics.SyncTransforms();

            foreach (var manager in AppEvent<IPhysicPostSimulate>.GetObjEvents())
            {
                manager.PostSimulate();
            }
        }
    }
}