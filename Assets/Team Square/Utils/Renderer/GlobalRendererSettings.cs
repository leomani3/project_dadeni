using UnityEngine;
using UnityEngine.Rendering;

namespace Utils.Rendering
{
    [CreateAssetMenu(menuName = "Config/Global Render Settings")]
    public class GlobalRenderSettings : ScriptableObject
    {
        public Material skyboxMaterial;
        public Color m_realTimeShadowColor = Color.black;

        public void Apply()
        {
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.subtractiveShadowColor = m_realTimeShadowColor;

                DynamicGI.UpdateEnvironment();
            }
        }
    }
}