using System.Collections;
using Farm.Core;
using Farm.Money;
using UnityEngine;

namespace Farm.Construction
{
    /// <summary>An empty plot (Box): spends currency to build, plays the box-open animation, then spawns its Construction (crop).</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ConstructionPlot : MonoBehaviour
    {
        [SerializeField] private CropConfig _config;
        [SerializeField] private Construction _constructionPrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private PrefabPool _buildDoneEffectPool;
        [SerializeField] private string _openClipName = "BoxOpen";

        private Animation _boxAnimation;
        private bool _built;

        public CropConfig Config => _config;
        public BigNumber BuildCost => new BigNumber(_config.BuildCost);

        private void Awake()
        {
            _boxAnimation = GetComponentInChildren<Animation>();

            if (GetComponent<Collider>() == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.center = new Vector3(0f, 0.5f, 0f);
                boxCollider.size = Vector3.one;
            }

            if (_spawnPoint == null) _spawnPoint = transform;
        }

        private void OnMouseDown()
        {
            if (_built) return;
            ConstructionUIController.Instance?.ShowBuildView(this);
        }

        public bool TryBuild(ICurrencyService currency)
        {
            if (_built) return false;
            if (!currency.TrySpend(CurrencyType.Cash, BuildCost)) return false;

            _built = true;
            StartCoroutine(OpenAndSpawn());
            return true;
        }

        private IEnumerator OpenAndSpawn()
        {
            float duration = _config.BoxOpenDuration;
            if (_boxAnimation != null)
            {
                _boxAnimation.Play(_openClipName);
                AnimationClip clip = _boxAnimation.clip;
                if (clip != null) duration = clip.length;
            }

            yield return new WaitForSeconds(duration);

            if (_buildDoneEffectPool != null)
            {
                GameObject effect = _buildDoneEffectPool.Rent(_spawnPoint.position, Quaternion.identity);
                _buildDoneEffectPool.Return(effect, 2f);
            }

            Instantiate(_constructionPrefab, _spawnPoint.position, _spawnPoint.rotation);
            Destroy(gameObject);
        }
    }
}
