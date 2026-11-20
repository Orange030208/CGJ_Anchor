using System;
using System.Collections;
using Anchor.Config;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Anchor.UI.Panel
{
    internal sealed class OnboardingTutorial : IDisposable
    {
        private static readonly string[] Lines =
        {
            "最近加班是不是太多了，怎么感觉刚才好像似了一下......",
            "不管那么多了，游戏马上上线，还是再复盘一下流程吧",
            "游戏每周都会汇总一下新增<color=#1554B0>愿望单</color>，这关系到游戏最终的销量",
            "我每周精力有限，需要好好规划一下开发进度",
            "这些数值代表游戏各方面的质量，<color=#A61938>能直接影响游戏发售后的结局</color>",
            "毕竟愿望单再高，游戏质量不行也是容易褒姒的",
            "随着开发的进行，<color=#9A4100>Bug每周都会增加</color>，记得让程序及时修！",
            "在对应房间分配精力则可以提升游戏质量或减少bug数量",
            "开发过程中每周都会遇到一些突发事件，可能会对游戏造成一些影响，得谨慎考虑一下",
            "按照以往经验，每个月都有神秘的奸商到访工作室，他的道具或许会影响我这个月的选择......",
            "当然，这是要花钱的。不过还好，<color=#1554B0>我们每个月都有追加预算!</color>",
            "每周项目都会结算一下开发进度和愿望单数量，帮助大家更好的复盘",
            "距离发售还剩<color=#1554B0>最后12周</color>，是时候冲刺了！"
        };

        private readonly TutorialOverlay overlay;
        private readonly MainPanelManager main;
        private readonly ButtonPrefabSpawner programRoom;
        private readonly FloatingUIFan programOptions;
        private readonly CinemachineScrollZoomController cameras;
        private readonly CinemachineBrain brain;
        private readonly EventPanelManager eventPanel;
        private readonly WindowShopPanelManager shop;
        private readonly WeekPanelManager week;
        private readonly EventSystem events;
        private readonly bool navigationEnabled;
        private readonly Vector3[] corners = new Vector3[4];
        private int line;
        private bool disposed;
        private bool previewClosed;
        private bool showingRoom;
        private bool showingEvent;
        private bool showingShop;
        private bool showingWeek;
        private bool completedByClick;

        internal bool Skipped { get; private set; }
        internal bool Finished { get; private set; }

        internal OnboardingTutorial(Sprite character, ButtonPrefabSpawner programRoom,
            CinemachineScrollZoomController cameras)
        {
            this.programRoom = programRoom;
            this.cameras = cameras;
            main = MainPanelManager.Instance;
            eventPanel = EventPanelManager.Instance;
            shop = WindowShopPanelManager.Instance;
            week = WeekPanelManager.Instance;
            programOptions = programRoom.GetComponentInChildren<FloatingUIFan>(true);
            Camera mainCamera = Camera.main;
            brain = mainCamera != null ? mainCamera.GetComponent<CinemachineBrain>() : null;
            events = EventSystem.current;

            TMP_FontAsset font = main.WishlistText != null ? main.WishlistText.font : TMP_Settings.defaultFontAsset;
            overlay = TutorialOverlay.Create(character, font, Skip);
            if (events != null)
            {
                navigationEnabled = events.sendNavigationEvents;
                events.SetSelectedGameObject(null);
                events.sendNavigationEvents = false;
            }

            main.Open();
        }

        internal IEnumerator Play()
        {
            overlay.PlayEntrance();
            ShowLine();
            yield return new WaitUntil(() => Finished);
        }

        private void Advance()
        {
            if (Finished)
            {
                return;
            }

            if (!completedByClick && (overlay.IsAnimating || IsPreviewAnimating()))
            {
                overlay.CompleteAnimations();
                CompletePreviewAnimations();
                completedByClick = true;
                return;
            }

            line++;
            if (line == Lines.Length)
            {
                Finished = true;
                overlay.PrepareExit();
                return;
            }

            ShowLine();
        }

        private void ShowLine()
        {
            completedByClick = false;
            Func<Rect?> highlight = null;
            switch (line)
            {
                case 2:
                    overlay.HideLetterbox();
                    highlight = FocusGraphics(main.WishlistRegion);
                    break;
                case 3:
                    highlight = () => new Rect(0.028f, 0.83f, 0.212f, 0.112f);
                    break;
                case 4:
                case 5:
                case 6:
                    highlight = FocusGraphics(main.StatsRegion);
                    break;
                case 7:
                    showingRoom = true;
                    programRoom.InvokeToggleButtonClick();
                    highlight = () => new Rect(0.34f, 0.02f, 0.60f, 0.94f);
                    break;
                case 8:
                    CloseRoom();
                    var exampleEvent = GameConfigs.Tables.TbgameEvent.GetOrDefault(2012);
                    showingEvent = eventPanel != null && eventPanel.Open(exampleEvent);
                    highlight = () => new Rect(0.035f, 0.025f, 0.93f, 0.92f);
                    break;
                case 9:
                    CloseEvent();
                    if (shop != null)
                    {
                        var buffs = GameConfigs.Tables.Tbbuff;
                        var featured = buffs.GetOrDefault(2001);
                        var offers = new[] { buffs.GetOrDefault(2003), featured, buffs.GetOrDefault(2010) };
                        showingShop = true;
                        shop.OpenPreview(offers, featured, 250000);
                        highlight = () => BoundsOf(shop.ShopRegion);
                    }
                    break;
                case 10:
                    if (showingShop)
                    {
                        highlight = FocusGraphics(shop.BudgetRegion);
                    }
                    break;
                case 11:
                    CloseShop();
                    if (week != null)
                    {
                        showingWeek = true;
                        week.OpenPreview(48, 0, 0, 300002);
                    }
                    highlight = () => new Rect(0.06f, 0.03f, 0.90f, 0.94f);
                    break;
                case 12:
                    CloseWeek();
                    break;
            }

            overlay.SetLine(Lines[line], highlight, Advance);
        }

        private bool IsPreviewAnimating()
        {
            return ((showingRoom || showingEvent) && brain != null && brain.IsBlending)
                || (showingRoom && programOptions != null && programOptions.IsTransitioning)
                || (showingEvent && eventPanel.IsOpening)
                || (showingShop && shop.IsOpening)
                || (showingWeek && week.IsOpening);
        }

        private void CompletePreviewAnimations()
        {
            if (brain != null && brain.IsBlending)
            {
                brain.ActiveBlend = null;
            }

            programOptions?.CompleteTransition();
            if (showingEvent) eventPanel.CompleteOpening();
            if (showingShop) shop.CompleteOpening();
            if (showingWeek) week.CompleteOpening();
        }

        private Func<Rect?> FocusGraphics(RectTransform root)
        {
            if (root == null) return null;
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>();

            return () =>
            {
                Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                foreach (Graphic graphic in graphics)
                {
                    if (graphic != null && graphic.isActiveAndEnabled)
                    {
                        IncludeBounds(graphic.rectTransform, ref min, ref max);
                    }
                }

                return min.x <= max.x ? ScreenBounds(min, max) : (Rect?)null;
            };
        }

        private Rect? BoundsOf(RectTransform target)
        {
            if (target == null) return null;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            IncludeBounds(target, ref min, ref max);
            return ScreenBounds(min, max);
        }

        private static Rect ScreenBounds(Vector2 min, Vector2 max)
        {
            Vector2 size = new Vector2(Screen.width, Screen.height);
            return Rect.MinMaxRect((min.x - 8f) / size.x, (min.y - 8f) / size.y,
                (max.x + 8f) / size.x, (max.y + 8f) / size.y);
        }

        private void IncludeBounds(RectTransform target, ref Vector2 min, ref Vector2 max)
        {
            Canvas canvas = target.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            target.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }

        private void Skip()
        {
            Skipped = true;
            Finished = true;
            overlay.PrepareExit();
        }

        private void CloseRoom()
        {
            if (!showingRoom) return;
            showingRoom = false;
            programRoom.CloseTargetObject();
            programOptions?.CompleteTransition();
            cameras.SwitchToDefaultCamera();
        }

        private void CloseEvent()
        {
            if (!showingEvent) return;
            showingEvent = false;
            eventPanel.Close();
        }

        private void CloseShop()
        {
            if (!showingShop) return;
            showingShop = false;
            shop.ClosePreview();
        }

        private void CloseWeek()
        {
            if (!showingWeek) return;
            showingWeek = false;
            week.Close();
        }

        internal void ClosePreview()
        {
            if (previewClosed) return;
            previewClosed = true;
            CloseRoom();
            CloseEvent();
            CloseShop();
            CloseWeek();
            if (main != null) main.Close();
            if (overlay != null)
            {
                overlay.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(overlay.gameObject);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            ClosePreview();
            if (events != null) events.sendNavigationEvents = navigationEnabled;
        }
    }
}
