using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Anchor.UI.Panel
{
    internal sealed class TutorialOverlay : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Vector2 PortraitRestPosition = new Vector2(-264f, 0f);
        private readonly Image[] shade = new Image[4];
        private readonly Image[] frame = new Image[4];
        private Image topBar;
        private Image bottomBar;
        private Image portrait;
        private Image dialogue;
        private TextMeshProUGUI text;
        private TMP_FontAsset font;
        private GameObject skip;
        private Sequence entrance;
        private Coroutine typing;
        private Func<Rect?> focus;
        private Action advance;
        private bool closing;
        private bool speaking;
        private float speechStartedAt;

        internal static TutorialOverlay Create(Sprite character, TMP_FontAsset font, Action onSkip)
        {
            var root = new GameObject("Tutorial", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32750;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var overlay = root.AddComponent<TutorialOverlay>();
            overlay.Build(character, font, onSkip);
            return overlay;
        }

        internal bool IsAnimating => typing != null || entrance.IsActive() && entrance.IsPlaying();

        internal void PlayEntrance() => entrance.Play();

        internal void SetLine(string line, Func<Rect?> highlight, Action onAdvance)
        {
            advance = onAdvance;
            focus = highlight;
            text.text = line;
            text.ForceMeshUpdate();
            text.maxVisibleCharacters = 0;
            typing = StartCoroutine(TypeLine());
        }

        internal void CompleteAnimations()
        {
            entrance.Complete(true);
            if (typing != null)
            {
                StopCoroutine(typing);
                typing = null;
            }
            text.maxVisibleCharacters = int.MaxValue;
            StopSpeaking();
        }

        internal void HideLetterbox()
        {
            topBar.enabled = false;
            bottomBar.enabled = false;
        }

        internal void PrepareExit()
        {
            closing = true;
            StopSpeaking();
            dialogue.gameObject.SetActive(false);
            skip.SetActive(false);
            focus = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!closing && eventData.button == PointerEventData.InputButton.Left)
            {
                advance?.Invoke();
            }
        }

        private void Build(Sprite character, TMP_FontAsset dialogueFont, Action onSkip)
        {
            font = dialogueFont;
            Image input = AddImage("Click to continue", transform, Color.clear);
            input.raycastTarget = true;
            input.canvasRenderer.cullTransparentMesh = false;
            Stretch(input.rectTransform, Vector2.zero, Vector2.one);
            for (int i = 0; i < shade.Length; i++)
            {
                shade[i] = AddImage("Dim", transform, new Color(0f, 0f, 0f, 0.65f));
                frame[i] = AddImage("Highlight edge", transform, new Color(0.48f, 0.88f, 1f, 0.9f));
            }

            topBar = AddImage("Top letterbox", transform, Color.black);
            bottomBar = AddImage("Bottom letterbox", transform, Color.black);
            Stretch(topBar.rectTransform, Vector2.up, Vector2.one);
            Stretch(bottomBar.rectTransform, Vector2.zero, Vector2.right);

            portrait = AddImage("Character", transform, Color.white);
            portrait.sprite = character;
            portrait.preserveAspect = true;
            RectTransform actor = portrait.rectTransform;
            actor.anchorMin = actor.anchorMax = new Vector2(1f, 0.22f);
            actor.sizeDelta = new Vector2(1460f, 1460f);
            actor.anchoredPosition = new Vector2(245f, 0f);
            actor.localRotation = Quaternion.Euler(0f, 180f, 0f);

            dialogue = AddImage("Dialogue", transform, new Color(0.64f, 0.83f, 0.96f, 0.93f));
            Stretch(dialogue.rectTransform, new Vector2(0.065f, 0.055f), new Vector2(0.935f, 0.23f));
            text = AddLabel("Line", dialogue.transform, new Color32(17, 30, 52, 255), 40, TextAlignmentOptions.MidlineLeft);
            text.rectTransform.offsetMin = new Vector2(36f, 13f);
            text.rectTransform.offsetMax = new Vector2(-36f, -13f);
            text.richText = true;
            text.enableAutoSizing = true;
            text.fontSizeMin = 26;
            text.fontSizeMax = 40;

            Image skipImage = AddImage("Skip", transform, new Color32(18, 34, 53, 245));
            skip = skipImage.gameObject;
            skipImage.raycastTarget = true;
            RectTransform buttonRect = skipImage.rectTransform;
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = Vector2.one;
            buttonRect.anchoredPosition = new Vector2(-22f, -12f);
            buttonRect.sizeDelta = new Vector2(132f, 48f);
            Button button = skip.AddComponent<Button>();
            button.targetGraphic = skipImage;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => onSkip());
            AddLabel("Label", skip.transform, Color.white, 27, TextAlignmentOptions.Center).text = "Skip";

            var dialogueFade = dialogue.gameObject.AddComponent<CanvasGroup>();
            dialogueFade.alpha = 0f;
            portrait.color = new Color(1f, 1f, 1f, 0f);
            entrance = DOTween.Sequence().SetUpdate(true).SetLink(gameObject).Pause();
            entrance.Append(topBar.rectTransform.DOAnchorMin(new Vector2(0f, 0.955f), 1.05f).SetEase(Ease.InOutSine));
            entrance.Join(bottomBar.rectTransform.DOAnchorMax(new Vector2(1f, 0.045f), 1.05f).SetEase(Ease.InOutSine));
            entrance.Append(actor.DOAnchorPosX(PortraitRestPosition.x, 1.85f).SetEase(Ease.InOutSine));
            entrance.Join(portrait.DOFade(1f, 1.7f).SetEase(Ease.InOutSine));
            entrance.Join(actor.DOAnchorPosY(12f, 0.185f).SetEase(Ease.InOutSine).SetLoops(10, LoopType.Yoyo));
            entrance.Append(dialogueFade.DOFade(1f, 0.24f));
            entrance.OnKill(() => entrance = null);
        }

        private IEnumerator TypeLine()
        {
            while (entrance.IsActive() && entrance.IsPlaying())
            {
                yield return null;
            }
            speechStartedAt = Time.unscaledTime;
            speaking = true;
            yield return DialogueTypewriter.Reveal(text, 30f, true);
            StopSpeaking();
            typing = null;
        }

        private void StopSpeaking()
        {
            speaking = false;
            portrait.rectTransform.anchoredPosition = PortraitRestPosition;
        }

        private void LateUpdate()
        {
            if (speaking)
            {
                float elapsed = Time.unscaledTime - speechStartedAt;
                float strength = Mathf.Min(elapsed * 12f, 1f);
                var motion = new Vector2(Mathf.Sin(elapsed * 39f) * 2f, Mathf.Sin(elapsed * 52f) * 3f);
                portrait.rectTransform.anchoredPosition = PortraitRestPosition + motion * strength;
            }

            Rect? target = focus?.Invoke();
            if (!target.HasValue)
            {
                Stretch(shade[0].rectTransform, Vector2.zero, Vector2.one);
                for (int i = 0; i < shade.Length; i++)
                {
                    shade[i].enabled = i == 0;
                    frame[i].enabled = false;
                }
                return;
            }

            Rect box = target.Value;
            Vector2 min = new Vector2(Mathf.Clamp01(box.xMin), Mathf.Clamp01(box.yMin));
            Vector2 max = new Vector2(Mathf.Clamp01(box.xMax), Mathf.Clamp01(box.yMax));
            Stretch(shade[0].rectTransform, Vector2.zero, new Vector2(min.x, 1f));
            Stretch(shade[1].rectTransform, new Vector2(max.x, 0f), Vector2.one);
            Stretch(shade[2].rectTransform, new Vector2(min.x, 0f), new Vector2(max.x, min.y));
            Stretch(shade[3].rectTransform, new Vector2(min.x, max.y), new Vector2(max.x, 1f));
            float dx = 2f / Mathf.Max(1, Screen.width);
            float dy = 2f / Mathf.Max(1, Screen.height);
            Stretch(frame[0].rectTransform, min, new Vector2(min.x + dx, max.y));
            Stretch(frame[1].rectTransform, new Vector2(max.x - dx, min.y), max);
            Stretch(frame[2].rectTransform, min, new Vector2(max.x, min.y + dy));
            Stretch(frame[3].rectTransform, new Vector2(min.x, max.y - dy), max);
            for (int i = 0; i < shade.Length; i++)
            {
                shade[i].enabled = true;
                frame[i].enabled = true;
            }
        }

        private static Image AddImage(string name, Transform parent, Color color)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI AddLabel(string name, Transform parent, Color color, float size, TextAlignmentOptions alignment)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            TextMeshProUGUI label = child.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            Stretch(label.rectTransform, Vector2.zero, Vector2.one);
            return label;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
