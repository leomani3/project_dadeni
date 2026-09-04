using UnityEngine;
using UnityEngine.Rendering;

namespace Utils.Rendering
{
    [ExecuteAlways]
    public class LightRig : MonoBehaviour
    {
        [SerializeField] private Light _keyLight;
        [SerializeField] private Light _fillLight;
        [SerializeField] private Light _rimLight;

        [SerializeField] private Transform _yawReference;
        [SerializeField] private bool _followReferenceYaw = true;

        [SerializeField] private float _keyYaw = 35f;
        [SerializeField] private float _keyPitch = 45f;
        [SerializeField] private Color _keyColor = new Color(1f, 0.937f, 0.827f, 1f);
        [SerializeField, Range(0f, 4f)] private float _keyIntensity = 1.5f;

        [SerializeField] private float _fillYaw = -110f;
        [SerializeField] private float _fillPitch = 25f;
        [SerializeField] private Color _fillColor = new Color(0.541f, 0.639f, 0.804f, 1f);
        [SerializeField, Range(0f, 4f)] private float _fillIntensity = 0.45f;

        [SerializeField] private float _rimYaw = 170f;
        [SerializeField] private float _rimPitch = 15f;
        [SerializeField] private Color _rimColor = new Color(0.784f, 0.867f, 1f, 1f);
        [SerializeField, Range(0f, 4f)] private float _rimIntensity = 0.8f;

        [SerializeField] private bool _overrideAmbient = true;
        [SerializeField] private Color _ambientSky = new Color(0.400f, 0.451f, 0.522f, 1f);
        [SerializeField] private Color _ambientEquator = new Color(0.298f, 0.278f, 0.290f, 1f);
        [SerializeField] private Color _ambientGround = new Color(0.157f, 0.129f, 0.129f, 1f);

        private bool _settingsChanged = true;

        private void OnEnable()
        {
            _settingsChanged = true;
        }

        private void OnValidate()
        {
            _settingsChanged = true;
        }

        private void LateUpdate()
        {
            bool tracksReference = _followReferenceYaw && _yawReference != null;

            if (!Application.isPlaying && !_settingsChanged)
                return;

            if (Application.isPlaying && !tracksReference && !_settingsChanged)
                return;

            Apply();
            _settingsChanged = false;
        }

        public void Apply()
        {
            float referenceYaw = _followReferenceYaw && _yawReference != null ? _yawReference.eulerAngles.y : 0f;

            ApplyLight(_keyLight, _keyPitch, _keyYaw + referenceYaw, _keyColor, _keyIntensity, LightShadows.Soft);
            ApplyLight(_fillLight, _fillPitch, _fillYaw + referenceYaw, _fillColor, _fillIntensity, LightShadows.None);
            ApplyLight(_rimLight, _rimPitch, _rimYaw + referenceYaw, _rimColor, _rimIntensity, LightShadows.None);

            if (!_overrideAmbient)
                return;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = _ambientSky;
            RenderSettings.ambientEquatorColor = _ambientEquator;
            RenderSettings.ambientGroundColor = _ambientGround;
        }

        private static void ApplyLight(Light _light, float _pitch, float _yaw, Color _color, float _intensity, LightShadows _shadows)
        {
            if (_light == null)
                return;

            _light.type = LightType.Directional;
            _light.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _light.color = _color;
            _light.intensity = _intensity;
            _light.shadows = _shadows;
        }
    }
}
