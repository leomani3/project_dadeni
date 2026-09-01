using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Utils.RendererEffect
{
    public class ColorRE : _RendererEffect
    {
        [TitleGroup("Settings")]
        [SerializeField, Range(0f, 1f)] private float m_alpha = 0.3f;

        [TitleGroup("Variables")]
        [SerializeField] private List<Color> originalColors = new List<Color>();


        [TitleGroup("Debug")]
        [Button]
        public void ChangeBaseColor(Color _newColor)
        {
            // this.Log($"Changing base color to {_newColor}.");
            var mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", _newColor);
                r.SetPropertyBlock(mpb);
            }
        }

        [Button]
        public void AddToBaseColorAndSetAlpha(Color _addColor)
        {
            var mpb = new MaterialPropertyBlock();
            int index = 0;
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", new Color(
                    originalColors[index].r + _addColor.r,
                    originalColors[index].g + _addColor.g,
                    originalColors[index].b + _addColor.b,
                    m_alpha));
                r.SetPropertyBlock(mpb);
                index++;
            }
        }

        [Button]
        public void SetAlpha(float _newAlpha)
        {
            var mpb = new MaterialPropertyBlock();

            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb); // Récupère le block UNE SEULE FOIS

                Color currentColor = mpb.GetColor("_BaseColor"); // Stocke la couleur

                mpb.SetColor("_BaseColor", new Color(
                    currentColor.r,
                    currentColor.g,
                    currentColor.b,
                    _newAlpha));

                r.SetPropertyBlock(mpb);
            }
            // this.Log($"Setting alpha of renderer to {_newAlpha}");
        }

        [Button]
        public void LerpColor(Color _targetColor)
        {
            var mpb = new MaterialPropertyBlock();
            int index = 0;
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                Color lerpedColor = Color.Lerp(mpb.GetColor("_BaseColor"), _targetColor, _targetColor.a);
                mpb.SetColor("_BaseColor", new Color(
                    lerpedColor.r,
                    lerpedColor.g,
                    lerpedColor.b,
                    mpb.GetColor("_BaseColor").a));
                r.SetPropertyBlock(mpb);
                index++;
            }
            // this.Log($"Setting alpha of renderers to {_targetColor}.");
        }



        [Button]
        public void Reset()
        {
            // this.Log("Resetting colors to original.");
            var mpb = new MaterialPropertyBlock();

            // if (originalColors != null && originalColors.Count != renderers.Count)
                // this.LogWarning("Original colors count does not match renderers count!");

            int idx = 0;
            foreach (var r in renderers)
            {
                if (r == null) { idx++; continue; }
                // this.Log($"Resetting color of renderer {r.name} to original color {originalColors[idx]}.");
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", originalColors[idx]);
                r.SetPropertyBlock(mpb);
                idx++;
            }
        }

        [Button]
        protected override void FillRenderers()
        {
            renderers = new List<Renderer>(GetComponentsInChildren<Renderer>());

            originalColors.Clear();
            var mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null)
                {
                    originalColors.Add(Color.black);
                    continue;
                }

                // Try to read the color from the renderer's material(s) first
                Color found = Color.black;
                bool foundInMaterial = false;
                var sharedMats = r.sharedMaterials;
                if (sharedMats != null)
                {
                    foreach (var mat in sharedMats)
                    {
                        if (mat == null) continue;
                        if (mat.HasProperty("_BaseColor"))
                        {
                            found = mat.GetColor("_BaseColor");
                            foundInMaterial = true;
                            break;
                        }
                        // Some shaders use _Color instead of _BaseColor
                        if (mat.HasProperty("_Color"))
                        {
                            found = mat.GetColor("_Color");
                            foundInMaterial = true;
                            break;
                        }
                    }
                }

                if (!foundInMaterial)
                    this.LogWarning($"RendererEffect: Could not find _BaseColor or _Color property in materials of renderer {r.name}. Trying to read from MaterialPropertyBlock.");

                originalColors.Add(found);
            }
        }
    }
}
