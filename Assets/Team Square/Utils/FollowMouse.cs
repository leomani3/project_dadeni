using UnityEngine;

public class FollowMouse : MonoBehaviour
{
    [SerializeField] private Vector3 m_offset;
    void Update()
    {
        transform.position = Input.mousePosition + m_offset;
    }
}