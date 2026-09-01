using UnityEngine;

namespace Utils.Rendering
{
    public class ApplyRenderSettingsFromAsset : MonoBehaviour
    {
        [SerializeField] private GlobalRenderSettings settings;

        private void Awake()
        {
            settings?.Apply();
        }
    }
}