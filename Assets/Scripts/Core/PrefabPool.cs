using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Farm.Core
{
    /// <summary>Reusable prefab instance pool (VFX, projectiles, etc.) so gameplay code never Instantiates/Destroys at runtime.</summary>
    public sealed class PrefabPool : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _prewarmCount = 4;

        private readonly Stack<GameObject> _pool = new Stack<GameObject>();

        private void Awake()
        {
            for (int i = 0; i < _prewarmCount; i++)
            {
                _pool.Push(CreateInstance());
            }
        }

        private GameObject CreateInstance()
        {
            GameObject instance = Instantiate(_prefab, transform);
            instance.SetActive(false);
            return instance;
        }

        public GameObject Rent(Vector3 position, Quaternion rotation)
        {
            GameObject instance = _pool.Count > 0 ? _pool.Pop() : CreateInstance();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(null);
            instance.SetActive(true);
            return instance;
        }

        public void Return(GameObject instance, float delay = 0f)
        {
            if (delay > 0f) StartCoroutine(ReturnAfterDelay(instance, delay));
            else ReturnNow(instance);
        }

        private IEnumerator ReturnAfterDelay(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnNow(instance);
        }

        private void ReturnNow(GameObject instance)
        {
            instance.SetActive(false);
            instance.transform.SetParent(transform);
            _pool.Push(instance);
        }
    }
}
