using UnityEngine;

public class DoorEnviro : MonoBehaviour
{
    [SerializeField] private GameObject enabledObject;
    [SerializeField] private GameObject disabledObject;

    public void SetEnabled(bool enabled)
    {
        enabledObject.SetActive(enabled);
        disabledObject.SetActive(!enabled);
    }
}