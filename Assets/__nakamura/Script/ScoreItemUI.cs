using UnityEngine;
using TMPro;

namespace ResultScene
{
    public class ScoreItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _scoreText;

        public void Setup(string itemName, int score)
        {
            if (_nameText != null)
                _nameText.text = itemName;

            if (_scoreText != null)
                _scoreText.text = score.ToString();
        }
    }
}
