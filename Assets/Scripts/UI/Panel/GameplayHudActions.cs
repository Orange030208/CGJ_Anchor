using System;
using Anchor.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Anchor.UI.Panel
{
    internal sealed class GameplayHudActions : MonoBehaviour
    {
        private TMP_FontAsset font;
        private GameObject confirmation;
        private EventSystem eventSystem;
        private bool navigationEnabled;

        internal static GameplayHudActions Create(Transform hud, TMP_FontAsset font, Sprite home, Sprite inventory)
        {
            var root = new GameObject("Game menu and inventory", typeof(RectTransform));
            root.transform.SetParent(hud, false);
            Stretch((RectTransform)root.transform);
            var actions = root.AddComponent<GameplayHudActions>();
            actions.font = font;
            actions.AddIcon("Return to main menu", home, 0.74f, actions.AskToReturn);
            actions.AddIcon("Inventory", inventory, 0.59f, actions.OpenInventory);
            return actions;
        }

        private void AddIcon(string label, Sprite sprite, float height, Action onClick)
        {
            Image image = AddImage(label, transform, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, height);
            rect.anchoredPosition = new Vector2(-88f, 0);
            rect.sizeDelta = new Vector2(78f, 78f);
            AddButton(image, onClick);
        }

        private void OpenInventory()
        {
            var board = GameFlowRunner.Instance?.Controller?.Blackboard;
            if (board != null)
            {
                WindowShopPanelManager.Instance?.OpenInventory(board.ActiveBuffIds, font);
            }
        }

        private void AskToReturn()
        {
            if (confirmation != null)
            {
                return;
            }
            confirmation = new GameObject("Return confirmation", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = confirmation.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20000;
            var scaler = confirmation.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                navigationEnabled = eventSystem.sendNavigationEvents;
                eventSystem.SetSelectedGameObject(null);
                eventSystem.sendNavigationEvents = false;
            }

            AddShade(confirmation.transform);
            Image border = AddImage("Confirmation frame", confirmation.transform, new Color32(162, 231, 245, 255));
            border.rectTransform.sizeDelta = new Vector2(760, 315);
            Image panel = AddImage("Confirmation panel", border.transform, new Color32(18, 30, 46, 255));
            Stretch(panel.rectTransform);
            panel.rectTransform.offsetMin = new Vector2(4, 4);
            panel.rectTransform.offsetMax = new Vector2(-4, -4);

            TextMeshProUGUI question = AddLabel("Return prompt", panel.transform, font, 43);
            question.text = "确定返回主菜单？";
            question.rectTransform.offsetMin = new Vector2(32, 160);
            question.rectTransform.offsetMax = new Vector2(-32, -25);
            TextMeshProUGUI detail = AddLabel("Current game warning", panel.transform, font, 27);
            detail.text = "返回后将结束本局游戏";
            detail.rectTransform.offsetMin = new Vector2(32, 95);
            detail.rectTransform.offsetMax = new Vector2(-32, -92);
            AddChoice(panel.transform, "是", -138, ConfirmReturn);
            AddChoice(panel.transform, "取消", 138, CloseConfirmation);
        }

        private void AddChoice(Transform parent, string label, float x, Action onClick)
        {
            Image image = AddImage(label, parent, new Color32(44, 75, 100, 255));
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            image.rectTransform.sizeDelta = new Vector2(214, 73);
            image.rectTransform.anchoredPosition = new Vector2(x, 58);
            AddButton(image, onClick);
            AddLabel("Label", image.transform, font, 34).text = label;
        }

        private void ConfirmReturn()
        {
            CloseConfirmation();
            GameFlowPanelCoordinator.GetOrCreate().ReturnToBeginPanel();
        }

        private void CloseConfirmation()
        {
            if (confirmation != null)
            {
                confirmation.SetActive(false);
                Destroy(confirmation);
                confirmation = null;
            }
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
                eventSystem.sendNavigationEvents = navigationEnabled;
                eventSystem = null;
            }
        }

        private void OnDisable()
        {
            CloseConfirmation();
            WindowShopPanelManager.Instance?.CloseInventory();
        }

        internal static Image AddShade(Transform parent)
        {
            Image shade = AddImage("Modal input blocker", parent, new Color32(0, 0, 0, 154));
            Stretch(shade.rectTransform);
            return shade;
        }

        internal static TextMeshProUGUI AddLabel(string name, Transform parent, TMP_FontAsset font, int size)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            var label = child.GetComponent<TextMeshProUGUI>();
            label.font = font != null ? font : TMP_Settings.defaultFontAsset;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            Stretch(label.rectTransform);
            return label;
        }

        private static Image AddImage(string name, Transform parent, Color color)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            var image = child.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void AddButton(Image image, Action onClick)
        {
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
