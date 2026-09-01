using UnityEngine;
using Sirenix.OdinInspector;
using Utils;
using System.Collections.Generic;

namespace Utils.RendererEffect
{
    public class _RendererEffect : MonoBehaviour
    {
        [TitleGroup("Dependencies")]
        [Required]
        [SerializeField] protected List<Renderer> renderers;

        [Button]
        protected virtual void FillRenderers()
        {
            renderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
        }
    }
}
