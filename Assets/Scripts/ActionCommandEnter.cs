using UnityEngine;

public class ActionCommandEnter : MonoBehaviour
{
    private TMPro.TMP_InputField _InputField;
    private void Awake()
    {
        _InputField = GetComponent<TMPro.TMP_InputField>();
        _InputField.onEndEdit.AddListener(GetCommand);
        _InputField.onSelect.AddListener(OnTextSelected);
        _InputField.onDeselect.AddListener(OnTextDeselect);
    }
    private void GetCommand(string pCommand)
    {
        CommandDecryptor.DecryptText(pCommand);
        _InputField.text = string.Empty;
    }
    private void OnTextSelected(string pText) => CommandDecryptor.CallOnCommandIsWritingEvent(true);
    private void OnTextDeselect(string pText) => CommandDecryptor.CallOnCommandIsWritingEvent(false);
    private void OnDestroy()
    {
        _InputField.onDeselect.RemoveAllListeners();
        _InputField.onEndEdit.RemoveAllListeners();
        _InputField.onSelect.RemoveAllListeners();        
    }
}