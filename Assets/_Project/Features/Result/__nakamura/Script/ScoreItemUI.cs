using TMPro;
using UnityEngine;

namespace ResultScene
{
    public class ScoreItemUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("項目名を表示するテキストUI")]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        [Tooltip("スコアを表示するテキストUI")]
        private TextMeshProUGUI _scoreText;

        public void Setup(string itemName, int score)
        {
            if (_nameText != null)
                _nameText.text = itemName;

            if (_scoreText != null)
                _scoreText.text = score.ToString();
        }
    }
}
