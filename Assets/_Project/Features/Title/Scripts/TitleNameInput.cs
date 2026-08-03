using TMPro;
using UnityEngine;

public class TitleNameInput : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _nameInputField;

    [SerializeField]
    private string _defaultName = "プレイヤー";

    private const string PlayerNameKey = "PLAYER_NAME";

    private void Start()
    {
        // 前回入力した名前があれば読み込み、無ければデフォルト名をセット
        if (_nameInputField != null)
        {
            _nameInputField.text = PlayerPrefs.GetString(PlayerNameKey, _defaultName);
            _nameInputField.onValueChanged.AddListener(SavePlayerName);
        }
    }

    /// <summary>
    /// 入力欄の文字が変わるたびにPlayerPrefsに保存
    /// </summary>
    public void SavePlayerName(string inputName)
    {
        string nameToSave = string.IsNullOrWhiteSpace(inputName) ? _defaultName : inputName;
        PlayerPrefs.SetString(PlayerNameKey, nameToSave);
        PlayerPrefs.Save();
    }
}
