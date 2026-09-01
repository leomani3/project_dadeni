using UnityEngine;
using UnityEngine.UI;

public class StringInput : MonoBehaviour
{
    [SerializeField] private Text inputField;
    [SerializeField] private Button validateButton;

    [SerializeField] private string fieldUse;


    private void Start() => SetUpInputField();


    private void SetUpInputField()
    {
        if (!PlayerPrefs.HasKey(fieldUse))
        {
            return;
        }

        string defaultName = PlayerPrefs.GetString(fieldUse);

        inputField.text = fieldUse;
        SetPlayerName(fieldUse);
    }

    private void SetPlayerName(string text)
    {
        validateButton.interactable = !string.IsNullOrEmpty(text);
    }
    
    public void SaveTextField()
    {
        string textField = inputField.text;

        PlayerPrefs.SetString(fieldUse, textField);
    }
}