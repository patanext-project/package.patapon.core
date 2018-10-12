using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace package.patapon.core
{
    public class CGraphicalUpdateMaskLookAnimator : MonoBehaviour
    {
        private GameObjectEntity m_GameObjectEntity;

        public int Id { get; private set; }

        public Vector2 Offset;

        private void Awake()
        {
            var referencable = ReferencableGameObject.GetComponent<ReferencableGameObject>(gameObject);
            m_GameObjectEntity = referencable.GetOrAddComponent<GameObjectEntity>();
        }
        
        private void OnEnable()
        {
            Id = transform.parent.GetInstanceID();
        }
    }
}