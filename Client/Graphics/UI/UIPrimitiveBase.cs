using System;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.Graphics.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIPrimitiveBase : MaskableGraphic
    {

        [SerializeField]
        private Sprite m_Sprite;

        public Sprite sprite
        {
            get => m_Sprite;
            set => SetAllDirty();
        }

        [NonSerialized]
        private Sprite m_OverrideSprite;

        public Sprite overrideSprite
        {
            get => m_OverrideSprite == null ? sprite : m_OverrideSprite;
            set => SetAllDirty();
        }

        // Not serialized until we support read-enabled sprites better.
        internal float m_EventAlphaThreshold = 1;

        public float eventAlphaThreshold
        {
            get => m_EventAlphaThreshold;
            set => m_EventAlphaThreshold = value;
        }

        /// <summary>
        /// Image's texture comes from the UnityEngine.Image.
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (overrideSprite == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }

                    return s_WhiteTexture;
                }

                return overrideSprite.texture;
            }
        }

        public float pixelsPerUnit
        {
            get
            {
                float spritePixelsPerUnit = 100;
                if (sprite)
                    spritePixelsPerUnit = sprite.pixelsPerUnit;

                float referencePixelsPerUnit = 100;
                if (canvas)
                    referencePixelsPerUnit = canvas.referencePixelsPerUnit;

                return spritePixelsPerUnit / referencePixelsPerUnit;
            }
        }


        protected UIVertex[] SetVbo(Vector2[] vertices, Vector2[] uvs)
        {
            UIVertex[] vbo = new UIVertex[4];
            for (int i = 0; i < vertices.Length; i++)
            {
                var vert = UIVertex.simpleVert;
                vert.color    = color;
                vert.position = vertices[i];
                vert.uv0      = uvs[i];
                vbo[i]        = vert;
            }

            return vbo;
        }
    }
}