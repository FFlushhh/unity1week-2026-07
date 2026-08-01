using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ResultScene.BuzzReaction
{
    [RequireComponent(typeof(Image))]
    public class HeartParticle : MonoBehaviour
    {
        private UIObjectPool _pool;
        private Image _image;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(
            UIObjectPool pool,
            Vector2 startPos,
            Vector2 initialVelocity,
            float gravity,
            float duration
        )
        {
            _pool = pool;
            _rectTransform.anchoredPosition = startPos;

            // 透明度をリセット
            Color c = _image.color;
            c.a = 1f;
            _image.color = c;

            StartCoroutine(AnimateRoutine(initialVelocity, gravity, duration));
        }

        private IEnumerator AnimateRoutine(Vector2 velocity, float gravity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 物理演算の更新（放物線運動ぶわーってやつ）
                velocity.y -= gravity * Time.deltaTime;
                _rectTransform.anchoredPosition += velocity * Time.deltaTime;

                // 後半の時間を使ってフェードアウトさせる
                if (elapsed > duration * 0.5f)
                {
                    float fadeRatio = (elapsed - duration * 0.5f) / (duration * 0.5f);
                    Color c = _image.color;
                    c.a = Mathf.Lerp(1f, 0f, fadeRatio);
                    _image.color = c;
                }

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
