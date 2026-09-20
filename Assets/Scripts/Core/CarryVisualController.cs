using System.Collections.Generic;
using UnityEngine;

namespace Farm.Core
{
    /// <summary>Shows up to one carried-product instance per anchor child, reusing instances per (anchor, prefab) pair.</summary>
    public sealed class CarryVisualController
    {
        private readonly Transform[] _anchors;
        private readonly Dictionary<(Transform anchor, GameObject prefab), GameObject> _cache = new Dictionary<(Transform, GameObject), GameObject>();
        private readonly List<GameObject> _activeVisuals = new List<GameObject>();

        public CarryVisualController(Transform carryRoot)
        {
            int count = carryRoot != null ? carryRoot.childCount : 0;
            _anchors = new Transform[count];
            for (int i = 0; i < count; i++) _anchors[i] = carryRoot.GetChild(i);
        }

        public void Show(GameObject productPrefab, int quantity)
        {
            Hide();
            if (productPrefab == null) return;

            int shown = Mathf.Min(quantity, _anchors.Length);
            for (int i = 0; i < shown; i++)
            {
                GameObject visual = GetOrCreate(_anchors[i], productPrefab);
                visual.SetActive(true);
                _activeVisuals.Add(visual);
            }
        }

        public void Hide()
        {
            foreach (GameObject visual in _activeVisuals) visual.SetActive(false);
            _activeVisuals.Clear();
        }

        private GameObject GetOrCreate(Transform anchor, GameObject prefab)
        {
            var key = (anchor, prefab);
            if (!_cache.TryGetValue(key, out GameObject instance))
            {
                instance = Object.Instantiate(prefab, anchor);
                instance.transform.localPosition = Vector3.zero;
                instance.SetActive(false);
                _cache[key] = instance;
            }

            return instance;
        }
    }
}
