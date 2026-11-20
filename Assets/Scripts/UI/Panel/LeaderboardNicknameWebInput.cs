#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using TMPro;
using UnityEngine;

namespace Anchor.UI.Panel
{
    internal sealed class LeaderboardNicknameWebInput : MonoBehaviour
    {
        public static void Open(MonoBehaviour dialog, TMP_InputField inputField)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var bridge = dialog.GetComponent<LeaderboardNicknameWebInput>();
            if (bridge == null)
                bridge = dialog.gameObject.AddComponent<LeaderboardNicknameWebInput>();
            bridge.Open(inputField);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void OpenLeaderboardNicknameInput(string target, string value, int maxLength);

        [DllImport("__Internal")]
        private static extern void PositionLeaderboardNicknameInput(float x, float y, float width, float height, float fontSize);

        [DllImport("__Internal")]
        private static extern void CloseLeaderboardNicknameInput();

        private TMP_InputField input;
        private Canvas canvas;
        private bool previousKeyboardCapture;
        private Vector4 lastRect;
        private float lastFontSize;

        private void Open(TMP_InputField field)
        {
            if (input != null)
                return;

            input = field;
            canvas = field.GetComponentInParent<Canvas>().rootCanvas;
            previousKeyboardCapture = WebGLInput.captureAllKeyboardInput;
            WebGLInput.captureAllKeyboardInput = false;
            OpenLeaderboardNicknameInput(gameObject.name, field.text, field.characterLimit);
            Canvas.ForceUpdateCanvases();
            lastRect = default;
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (input == null)
                return;

            RectTransform viewport = input.textViewport;
            Rect rect = viewport.rect;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(rect.min));
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(rect.max));
            var nextRect = new Vector4(min.x / Screen.width, 1f - max.y / Screen.height,
                (max.x - min.x) / Screen.width, (max.y - min.y) / Screen.height);
            float fontSize = input.textComponent.fontSize * canvas.scaleFactor / Screen.height;
            if (nextRect == lastRect && Mathf.Approximately(fontSize, lastFontSize))
                return;

            lastRect = nextRect;
            lastFontSize = fontSize;
            PositionLeaderboardNicknameInput(nextRect.x, nextRect.y, nextRect.z, nextRect.w, fontSize);
        }

        private void OnDisable()
        {
            if (input == null)
                return;

            CloseLeaderboardNicknameInput();
            WebGLInput.captureAllKeyboardInput = previousKeyboardCapture;
            input = null;
        }
#endif
    }
}
