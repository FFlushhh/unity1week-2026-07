using System.Collections.Generic;
using UnityEngine;

namespace ResultScene.BuzzReaction
{
    public class UIObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();
        private readonly HashSet<GameObject> _activeObjects = new HashSet<GameObject>();

        public UIObjectPool(GameObject prefab, Transform parent, int initialCapacity = 20)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < initialCapacity; i++)
            {
                var obj = Object.Instantiate(_prefab, _parent);
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        public GameObject Get()
        {
            if (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                obj.SetActive(true);
                // UIの前面に描画されるように、ヒエラルキーの最後尾（一番下）へ移動
                obj.transform.SetAsLastSibling();
                _activeObjects.Add(obj);
                return obj;
            }
            else
            {
                var obj = Object.Instantiate(_prefab, _parent);
                obj.SetActive(true);
                _activeObjects.Add(obj);
                return obj;
            }
        }

        public void ReturnToPool(GameObject obj)
        {
            if (!_activeObjects.Remove(obj))
                return;

            obj.SetActive(false);
            _pool.Enqueue(obj);
        }

        public void ReturnAllToPool()
        {
            foreach (GameObject obj in _activeObjects)
            {
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }

            _activeObjects.Clear();
        }
    }
}
