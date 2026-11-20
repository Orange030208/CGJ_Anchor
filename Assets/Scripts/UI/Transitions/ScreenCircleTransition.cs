using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Anchor.UI.Transitions
{
    /// <summary>
    /// Full-screen black transition that reveals the next UI through an expanding circular opening.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenCircleTransition : MonoBehaviour
    {
        private const string TransitionObjectName = "ScreenCircleTransition";
        private const string ShaderResourcePath = "Shaders/ScreenCircleTransition";
        private static ScreenCircleTransition instance;

        [Header("Timing")]
        [SerializeField, Min(0f)]
        private float fadeToBlackDuration = 0.5f;

        [SerializeField, Min(0f)]
        private float revealDuration = 1.2f;

        [SerializeField, Min(0f)]
        private float blackHoldDuration = 0.12f;

        [Header("Circle")]
        [SerializeField, Range(0f, 0.25f)]
        private float initialCircleRadius = 0.035f;

        [SerializeField, Range(0f, 0.2f)]
        private float edgeSoftness = 0.025f;

        [SerializeField]
        private Vector2 circleCenter = new Vector2(0.5f, 0.5f);

        [Header("Overlay")]
        [SerializeField]
        private Color overlayColor = Color.black;

        [SerializeField]
        private int sortingOrder = 32760;

        private Canvas overlayCanvas;
        private RawImage overlayImage;
        private Material overlayMaterial;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;

        public static ScreenCircleTransition GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindObjectOfType<ScreenCircleTransition>(true);
            if (instance != null)
            {
                return instance;
            }

            var transitionObject = new GameObject(TransitionObjectName);
            DontDestroyOnLoad(transitionObject);
            instance = transitionObject.AddComponent<ScreenCircleTransition>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureOverlay();
            HideOverlay();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            if (overlayMaterial != null)
            {
                Destroy(overlayMaterial);
            }
        }

        public IEnumerator Play(System.Action whileBlack)
        {
            yield return Play(whileBlack, closeCircle: false);
        }

        public IEnumerator PlayClosingCircle(System.Action whileBlack)
        {
            yield return Play(whileBlack, closeCircle: true);
        }

        public IEnumerator PlayFade(System.Action whileBlack)
        {
            yield return Play(whileBlack, closeCircle: false, fadeReveal: true);
        }

        private IEnumerator Play(System.Action whileBlack, bool closeCircle, bool fadeReveal = false)
        {
            if (isPlaying)
            {
                yield break;
            }

            isPlaying = true;
            EnsureOverlay();
            ShowOverlay();

            if (closeCircle && overlayMaterial != null)
            {
                yield return AnimateRadius(CalculateCoverRadius(), -edgeSoftness, fadeToBlackDuration);
            }
            else
            {
                yield return AnimateOpacity(0f, 1f, fadeToBlackDuration);
            }

            whileBlack?.Invoke();

            if (blackHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(blackHoldDuration);
            }

            if (fadeReveal)
            {
                yield return AnimateOpacity(1f, 0f, fadeToBlackDuration);
            }
            else
            {
                yield return AnimateRadius(initialCircleRadius, CalculateCoverRadius(), revealDuration);
            }
            HideOverlay();
            isPlaying = false;
        }

        private IEnumerator AnimateOpacity(float from, float to, float duration)
        {
            SetOverlayState(from, -edgeSoftness);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetOverlayState(Mathf.Lerp(from, to, Smooth01(t)), -edgeSoftness);
                yield return null;
            }

            SetOverlayState(to, -edgeSoftness);
        }

        private IEnumerator AnimateRadius(float startRadius, float endRadius, float duration)
        {
            SetOverlayState(1f, startRadius);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float radius = Mathf.Lerp(startRadius, endRadius, Smooth01(t));
                SetOverlayState(1f, radius);
                yield return null;
            }

            SetOverlayState(1f, endRadius);
        }

        private void EnsureOverlay()
        {
            if (overlayCanvas == null)
            {
                overlayCanvas = gameObject.GetComponent<Canvas>();
                if (overlayCanvas == null)
                {
                    overlayCanvas = gameObject.AddComponent<Canvas>();
                }

                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.overrideSorting = true;
                overlayCanvas.sortingOrder = sortingOrder;
            }

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (overlayImage == null)
            {
                Transform existingImage = transform.Find("Overlay");
                if (existingImage != null)
                {
                    overlayImage = existingImage.GetComponent<RawImage>();
                }
            }

            if (overlayImage == null)
            {
                var imageObject = new GameObject("Overlay");
                imageObject.transform.SetParent(transform, false);
                overlayImage = imageObject.AddComponent<RawImage>();
                overlayImage.raycastTarget = true;

                RectTransform rectTransform = overlayImage.rectTransform;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            EnsureMaterial();
            overlayImage.material = overlayMaterial;
        }

        private void EnsureMaterial()
        {
            if (overlayMaterial != null)
            {
                return;
            }

            Shader shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader == null)
            {
                Debug.LogWarning($"{nameof(ScreenCircleTransition)} cannot load shader Resources/{ShaderResourcePath}.");
                return;
            }

            overlayMaterial = new Material(shader)
            {
                name = $"{TransitionObjectName}Material",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void SetOverlayState(float alpha, float radius)
        {
            Color color = overlayColor;
            color.a = alpha;
            overlayImage.color = color;

            if (overlayMaterial == null)
            {
                return;
            }

            overlayMaterial.SetColor("_Color", color);
            overlayMaterial.SetVector("_HoleCenter", new Vector4(circleCenter.x, circleCenter.y, 0f, 0f));
            overlayMaterial.SetFloat("_HoleRadius", radius);
            overlayMaterial.SetFloat("_Softness", edgeSoftness);
            overlayMaterial.SetFloat("_Aspect", GetScreenAspect());
        }

        private float CalculateCoverRadius()
        {
            float aspect = GetScreenAspect();
            float left = Mathf.Abs(circleCenter.x) * aspect;
            float right = Mathf.Abs(1f - circleCenter.x) * aspect;
            float bottom = Mathf.Abs(circleCenter.y);
            float top = Mathf.Abs(1f - circleCenter.y);
            float horizontal = Mathf.Max(left, right);
            float vertical = Mathf.Max(bottom, top);
            return Mathf.Sqrt(horizontal * horizontal + vertical * vertical) + edgeSoftness;
        }

        private static float GetScreenAspect()
        {
            return Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
        }

        private static float Smooth01(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private void ShowOverlay()
        {
            overlayCanvas.enabled = true;
            overlayImage.enabled = true;
            overlayImage.raycastTarget = true;
        }

        private void HideOverlay()
        {
            if (overlayCanvas != null)
            {
                overlayCanvas.enabled = false;
            }

            if (overlayImage != null)
            {
                overlayImage.enabled = false;
                overlayImage.raycastTarget = false;
            }
        }
    }
}
