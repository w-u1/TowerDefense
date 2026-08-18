using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 特效对象池。复用死亡爆炸、炮口闪光、命中特效等高频特效对象。
    /// 单例模式，自动创建。
    /// </summary>
    public class EffectsPool : MonoBehaviour
    {
        public static EffectsPool Instance { get; private set; }

        [Header("池大小")]
        [SerializeField] private int _deathEffectPoolSize = 20;
        [SerializeField] private int _muzzleFlashPoolSize = 30;
        [SerializeField] private int _hitEffectPoolSize = 30;

        private readonly Queue<GameObject> _deathEffects = new Queue<GameObject>();
        private readonly Queue<GameObject> _muzzleFlashes = new Queue<GameObject>();
        private readonly Queue<GameObject> _hitEffects = new Queue<GameObject>();

        private GameObject _deathEffectPrefab;
        private GameObject _muzzleFlashPrefab;
        private GameObject _hitEffectPrefab;

        private Transform _container;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            var go = new GameObject("EffectsPool");
            _container = go.transform;
            _container.SetParent(transform);

            PrewarmPools();
        }

        private void PrewarmPools()
        {
            _deathEffectPrefab = CreateDeathEffectTemplate();
            for (int i = 0; i < _deathEffectPoolSize; i++)
            {
                var obj = Instantiate(_deathEffectPrefab, _container);
                obj.SetActive(false);
                _deathEffects.Enqueue(obj);
            }

            _muzzleFlashPrefab = CreateMuzzleFlashTemplate();
            for (int i = 0; i < _muzzleFlashPoolSize; i++)
            {
                var obj = Instantiate(_muzzleFlashPrefab, _container);
                obj.SetActive(false);
                _muzzleFlashes.Enqueue(obj);
            }

            _hitEffectPrefab = CreateHitEffectTemplate();
            for (int i = 0; i < _hitEffectPoolSize; i++)
            {
                var obj = Instantiate(_hitEffectPrefab, _container);
                obj.SetActive(false);
                _hitEffects.Enqueue(obj);
            }
        }

        public void SpawnDeathEffect(Vector3 position, float size = 0.55f)
        {
            GameObject obj;
            if (_deathEffects.Count > 0)
                obj = _deathEffects.Dequeue();
            else
                obj = Instantiate(_deathEffectPrefab, _container);

            var anim = obj.GetComponent<Enemies.DeathEffectAnim>();
            if (anim != null)
            {
                anim.Play(position, size);
            }
            else
            {
                obj.transform.position = position;
                obj.SetActive(true);
            }
            StartCoroutine(ReturnAfterDelay(obj, _deathEffects, 0.4f));
        }

        public void SpawnMuzzleFlash(Vector3 position, Color color, float size = 0.5f)
        {
            GameObject obj;
            if (_muzzleFlashes.Count > 0)
                obj = _muzzleFlashes.Dequeue();
            else
                obj = Instantiate(_muzzleFlashPrefab, _container);

            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * size;
            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;
            obj.SetActive(true);

            StartCoroutine(ReturnAfterDelay(obj, _muzzleFlashes, 0.1f));
        }

        public void SpawnHitEffect(Vector3 position, Color color, float size = 0.7f, float duration = 0.35f)
        {
            GameObject obj;
            if (_hitEffects.Count > 0)
                obj = _hitEffects.Dequeue();
            else
                obj = Instantiate(_hitEffectPrefab, _container);

            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * size * 0.5f;
            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;
            obj.SetActive(true);

            StartCoroutine(AnimateHitEffect(obj, size, duration));
        }

        private System.Collections.IEnumerator AnimateHitEffect(GameObject obj, float targetSize, float duration)
        {
            float t = 0;
            Vector3 startScale = obj.transform.localScale;
            var sr = obj.GetComponent<SpriteRenderer>();
            Color startColor = sr != null ? sr.color : Color.white;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                obj.transform.localScale = Vector3.Lerp(startScale, Vector3.one * targetSize, p);
                if (sr != null)
                {
                    Color c = startColor;
                    c.a = startColor.a * (1f - p);
                    sr.color = c;
                }
                yield return null;
            }

            obj.SetActive(false);
            _hitEffects.Enqueue(obj);
        }

        private System.Collections.IEnumerator ReturnAfterDelay(GameObject obj, Queue<GameObject> pool, float delay)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(false);
            if (!pool.Contains(obj))
                pool.Enqueue(obj);
        }

        private GameObject CreateDeathEffectTemplate()
        {
            var go = new GameObject("DeathEffect");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateExplosionSprite(64);
            sr.sortingOrder = 12;
            go.AddComponent<Enemies.DeathEffectAnim>();
            return go;
        }

        private GameObject CreateMuzzleFlashTemplate()
        {
            var go = new GameObject("MuzzleFlash");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateStarSprite(32);
            sr.sortingOrder = 10;
            return go;
        }

        private GameObject CreateHitEffectTemplate()
        {
            var go = new GameObject("HitEffect");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateRingSprite(48);
            sr.sortingOrder = 8;
            return go;
        }

        /// <summary>多层径向渐变爆炸精灵（外圈红→中圈橙→内圈黄→核心白）</summary>
        private static Sprite GenerateExplosionSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    int idx = y * size + x;
                    if (dist > radius) { pixels[idx] = Color.clear; continue; }

                    float t = dist / radius;
                    Color c;
                    if (t < 0.2f) c = Color.Lerp(new Color(1f, 1f, 0.9f), new Color(1f, 0.95f, 0.5f), t / 0.2f);
                    else if (t < 0.5f) c = Color.Lerp(new Color(1f, 0.95f, 0.5f), new Color(1f, 0.6f, 0.2f), (t - 0.2f) / 0.3f);
                    else c = Color.Lerp(new Color(1f, 0.6f, 0.2f), new Color(0.8f, 0.2f, 0.1f, 0f), (t - 0.5f) / 0.5f);

                    // 边缘抗锯齿
                    float edge = radius - dist;
                    if (edge < 2f) c.a *= Mathf.Clamp01(edge * 0.5f);
                    pixels[idx] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>四角星形闪光精灵</summary>
        private static Sprite GenerateStarSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    float dist = Mathf.Max(dx, dy);
                    float diag = (dx + dy) * 0.707f;
                    float starDist = Mathf.Min(dist, diag * 1.6f);

                    if (starDist > size * 0.45f) { pixels[idx] = Color.clear; continue; }

                    float t = starDist / (size * 0.45f);
                    Color c = Color.Lerp(new Color(1f, 1f, 0.8f, 1f), new Color(1f, 0.7f, 0.2f, 0f), t);
                    float edge = size * 0.45f - starDist;
                    if (edge < 1.5f) c.a *= Mathf.Clamp01(edge / 1.5f);
                    pixels[idx] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>命中特效：双层发光环+中心闪光点</summary>
        private static Sprite GenerateRingSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerR = size * 0.45f;
            float midR = size * 0.32f;
            float innerR = size * 0.2f;
            float coreR = size * 0.08f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    Color c = Color.clear;

                    // 外环（粗）
                    if (dist <= outerR && dist >= midR)
                    {
                        float t = (dist - midR) / (outerR - midR);
                        c = Color.Lerp(new Color(1f, 1f, 1f, 0.9f), new Color(1f, 1f, 1f, 0f), t);
                    }
                    // 内环（细）
                    else if (dist <= innerR && dist >= innerR * 0.6f)
                    {
                        c = new Color(1f, 1f, 1f, 0.7f);
                    }
                    // 中心发光核心
                    else if (dist <= coreR)
                    {
                        float t = dist / coreR;
                        c = Color.Lerp(new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0.3f), t);
                    }

                    // 边缘抗锯齿
                    float edge = outerR - dist;
                    if (edge < 1.5f && edge > 0) c.a *= Mathf.Clamp01(edge / 1.5f);
                    pixels[idx] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public void ClearAll()
        {
            foreach (Transform child in _container)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
