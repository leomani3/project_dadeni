using UnityEngine;


namespace Utils.UI
{
    public class WorlToScrrenPoint : MonoBehaviour
    {
        [SerializeField] private GameObject ui_obj;
        [SerializeField] private Transform target_trans;
        [SerializeField] private Vector3 offSet;

        public virtual void FixedUpdate()
        {
            ui_obj.transform.position = Camera.main.WorldToScreenPoint(target_trans.position + offSet);
        }
    }
}
