using UnityEngine;

[ExecuteAlways]
public class Door : MonoBehaviour
{
    [SerializeField] private DoorEnviro _doorEnviro;

    public void SetEnabled(bool enabled)
    {
        gameObject.SetActive(enabled);
    }

    private void OnEnable()
    {
        if (_doorEnviro != null)
            _doorEnviro.SetEnabled(true);
    }

    private void OnDisable()
    {
        if (_doorEnviro != null)
            _doorEnviro.SetEnabled(false);
    }
}
