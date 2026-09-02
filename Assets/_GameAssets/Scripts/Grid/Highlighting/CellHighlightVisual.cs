using UnityEngine;
using UnityEngine.Rendering;

namespace Deckbuilder.Grid.Highlighting
{
    public class CellHighlightVisual : MonoBehaviour
    {
        private const int SlotCount = 3;
        private static readonly float[] SlotScale = { 0.95f, 0.7f, 0.45f };
        private static readonly float[] SlotHeight = { 0.02f, 0.03f, 0.04f };

        private static Material m_sharedMaterial;

        private Renderer[] m_slotRenderers;
        private MaterialPropertyBlock m_propertyBlock;

        public void SetSlot(int _slotIndex, Color? _color)
        {
            EnsureBuilt();

            if (_slotIndex < 0 || _slotIndex >= SlotCount)
                return;

            Renderer _renderer = m_slotRenderers[_slotIndex];

            if (_color == null)
            {
                _renderer.gameObject.SetActive(false);
                return;
            }

            m_propertyBlock.SetColor("_BaseColor", _color.Value);
            m_propertyBlock.SetColor("_Color", _color.Value);
            _renderer.SetPropertyBlock(m_propertyBlock);
            _renderer.gameObject.SetActive(true);
        }

        private void EnsureBuilt()
        {
            if (m_slotRenderers != null)
                return;

            if (m_sharedMaterial == null)
                m_sharedMaterial = CreateSharedMaterial();

            m_propertyBlock = new MaterialPropertyBlock();
            m_slotRenderers = new Renderer[SlotCount];

            float _cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;

            for (int _i = 0; _i < SlotCount; _i++)
            {
                GameObject _quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _quad.name = $"HighlightSlot_{_i}";
                _quad.transform.SetParent(transform, false);
                _quad.transform.localPosition = new Vector3(0f, SlotHeight[_i], 0f);
                _quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _quad.transform.localScale = Vector3.one * (_cellSize * SlotScale[_i]);

                Collider _collider = _quad.GetComponent<Collider>();
                if (_collider != null)
                    Destroy(_collider);

                Renderer _renderer = _quad.GetComponent<Renderer>();
                _renderer.sharedMaterial = m_sharedMaterial;
                _renderer.shadowCastingMode = ShadowCastingMode.Off;
                _renderer.receiveShadows = false;

                _quad.SetActive(false);
                m_slotRenderers[_i] = _renderer;
            }
        }

        private static Material CreateSharedMaterial()
        {
            Shader _shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (_shader == null)
                _shader = Shader.Find("Sprites/Default");

            Material _material = new Material(_shader);

            if (_material.HasProperty("_Surface"))
            {
                _material.SetFloat("_Surface", 1f);
                _material.SetFloat("_Blend", 0f);
                _material.renderQueue = (int)RenderQueue.Transparent;
                _material.SetOverrideTag("RenderType", "Transparent");
                _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            return _material;
        }
    }
}
