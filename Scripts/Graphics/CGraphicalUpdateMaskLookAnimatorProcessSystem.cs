using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Jobs;

namespace package.patapon.core
{
    [UpdateAfter(typeof(PreLateUpdate.DirectorUpdateAnimationEnd))]
    public class CGraphicalUpdateMaskLookAnimatorProcessSystem : ComponentSystem
    {
        public const string AnimatorAimlookXParameter = "AimlookX";
        public const string AnimatorAimlookYParameter = "AimlookY";

        struct Group
        {
            public ComponentArray<CGraphicalUpdateMaskLookAnimator> MaskArray;
            public ComponentArray<Animator>                         AnimatorArray;
            public TransformAccessArray                             Transforms;

            public readonly int Length;
        }

        [Inject] private Group m_Group;

        private int m_HashedAimlookXParameter;
        private int m_HashedAimlookYParameter;

        protected override void OnCreateManager()
        {
            m_HashedAimlookXParameter = Animator.StringToHash(AnimatorAimlookXParameter);
            m_HashedAimlookYParameter = Animator.StringToHash(AnimatorAimlookYParameter);
        }

        protected override void OnUpdate()
        {
            for (int i = 0; i != m_Group.Length; i++)
            {
                var mask      = m_Group.MaskArray[i];
                var animator  = m_Group.AnimatorArray[i];
                var transform = m_Group.Transforms[i];

                var eyeDetail = CGraphicalStackEyeDetail.Get(mask.Id);
                var targetOffsetPosition = eyeDetail == null ? Vector2.zero : eyeDetail.PositionOffset;
                var targetAim = eyeDetail == null ? Vector2.zero : eyeDetail.Aim;

                targetAim.x = math.min(targetAim.x, 1);
                targetAim.y = math.min(targetAim.y, 1);

                animator.SetFloat(m_HashedAimlookXParameter, targetAim.x);
                animator.SetFloat(m_HashedAimlookYParameter, targetAim.y);

                animator.speed = 0f;
                animator.Update(Time.deltaTime);
            }
        }
    }
}