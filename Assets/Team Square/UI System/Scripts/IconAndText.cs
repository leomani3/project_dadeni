using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Utils.UI;

public enum TextFormat
{
    None,
    SmartString,
}

public class IconAndText : AUIElement
{
    [TitleGroup("Dependencies")]
    [SerializeField, Required] protected Image m_icon;
    [SerializeField, Required] protected TMP_Text m_valueText;

    private Color m_initialColor;

    private void Awake()
    {
        m_initialColor = m_valueText.color;
    }
    
    public virtual void SetIcon(Sprite _icon)
    {
        m_icon.sprite = _icon;
    }

    public virtual void SetValue(string _text)
    {
        m_valueText.text = _text;
    }

    public virtual void SetValue(double _value, TextFormat format = TextFormat.None)
    {
        switch (format)
        {
            case TextFormat.SmartString:
                m_valueText.text = _value.ToSmartString();
                break;
            default:
                m_valueText.text = _value.ToString();
                break;
        }
    }

    public virtual void SetText(string _text)
    {
        m_valueText.text = _text;
    }

    public virtual void SetTextColor(Color? color = null)
    {
        if (color.HasValue)
            m_valueText.color = color.Value;
        else
            m_valueText.color = m_initialColor;
    }
}