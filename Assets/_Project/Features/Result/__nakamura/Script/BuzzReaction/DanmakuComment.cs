using System.Collections;
using TMPro;
using UnityEngine;

namespace ResultScene.BuzzReaction
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DanmakuComment : MonoBehaviour
    {
        private UIObjectPool _pool;
        private TextMeshProUGUI _textMesh;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _textMesh = GetComponent<TextMeshProUGUI>();
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(
            UIObjectPool pool,
            string text,
            Vector2 startPos,
            float speed,
            float endX
        )
        {
            _pool = pool;
            _textMesh.text = text;
            _rectTransform.anchoredPosition = startPos;

            // 必要に応じて色をリセット（透明度を元に戻す）
            Color c = _textMesh.color;
            c.a = 1f;
            _textMesh.color = c;

            StartCoroutine(AnimateRoutine(speed, endX));
        }

        private IEnumerator AnimateRoutine(float speed, float endX)
        {
            // 左方向へ移動させる
            while (_rectTransform.anchoredPosition.x > endX)
            {
                _rectTransform.anchoredPosition += Vector2.left * speed * Time.deltaTime;
                yield return null;
            }

            if (_pool != null)
            {
                _pool.ReturnToPool(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
