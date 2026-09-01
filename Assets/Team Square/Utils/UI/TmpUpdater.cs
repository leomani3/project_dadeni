using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using System.Collections;


public class TmpUpdater : MonoBehaviour
{
    [TitleGroup("Dependencies")]
    [Required]
    [SerializeField] private TextMeshProUGUI m_tmp;

    [TitleGroup("Settings")]
    [SerializeField] private string m_prefix = "";
    [SerializeField] private float m_reachLerp = 0.1f;
    [SerializeField] private Color m_defaultColor = Color.white;


    [TitleGroup("Variables")]
    [ReadOnly]
    [SerializeField] private string m_currrentText = "";

    [ReadOnly]
    [SerializeField] private double m_targetValue = 0;
    [ReadOnly]
    [SerializeField] private double m_currentValue = 0;
    private Coroutine m_reachValueCoroutine = null;


    public void SetText(string _newText)
    {
        string newText = m_prefix + _newText;
        if (m_currrentText != newText)
        {
            m_currrentText = newText;
            m_tmp.text = newText;
        }
    }

    public void SetTargetValue(double _newValue, bool _immediate = false)
    {
        m_targetValue = _newValue;
        if (_immediate)
        {
            m_currentValue = _newValue;
            SetText(m_currentValue.ToString("0"));
            if (m_reachValueCoroutine != null)
            {
                StopCoroutine(m_reachValueCoroutine);
                m_reachValueCoroutine = null;
            }
        }
        else if (m_reachValueCoroutine == null)
        {
            m_reachValueCoroutine = StartCoroutine(ReachValueCo());
        }
    }

    private IEnumerator ReachValueCo()
    {
        while (m_currentValue != m_targetValue)
        {
            m_currentValue = Mathf.Lerp((float)m_currentValue, (float)m_targetValue, m_reachLerp);
            SetText(m_currentValue.ToString("0"));
            yield return new WaitForEndOfFrame();
        }
        m_reachValueCoroutine = null;
    }


    public void SetColor(Color _newColor)
    {
        m_tmp.color = _newColor;
    }
    
    public void ResetColor()
    {
        m_tmp.color = m_defaultColor;
    }
}
