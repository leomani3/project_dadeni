using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


namespace Utils.RendererEffect
{
    public class ShinyRE : _RendererEffect
    {
        [TitleGroup("Settings")]
        [ColorUsage(true, true)]
        [SerializeField] private Vector2 rimOsillation = new Vector2(0.0f, 0.3f);
        [SerializeField, Min(0.01f)] private float rimFrequency = 2f;

        [TitleGroup("Variables")]
        [ReadOnly]
        [SerializeField] private bool isHighlighted = false;
        [ReadOnly]
        [SerializeField] private float t;

        void Update()
        {
            if (isHighlighted)
                RimOsillation();
        }

        [Button]
        public void Highlight(Color emissionColor)
        {
            // this.Log("Highlighting with color: " + emissionColor);
            if (isHighlighted)
                return;

            var mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_Emission", emissionColor);
                r.SetPropertyBlock(mpb);
            }

            isHighlighted = true;
        }

        [Button]
        public void Unhighlight()
        {
            // this.Log("Unhighlighting.");
            if (!isHighlighted)
                return;

            var mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_Emission", Color.black);
                mpb.SetFloat("_RimMin", 1f);
                r.SetPropertyBlock(mpb);
            }

            isHighlighted = false;
        }

        private void RimOsillation()
        {
            // Oscille la valeur _RimMin entre rimOsillation.x (min) et rimOsillation.y (max)
            // en utilisant une sinusoide basée sur Time.time.
            t = (Mathf.Sin(Time.time * rimFrequency) + 1f) * 0.5f; // 0..1
            float rimValue = Mathf.Lerp(rimOsillation.x, rimOsillation.y, t);

            var mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_RimMin", rimValue);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
