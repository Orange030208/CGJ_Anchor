using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Anchor.GameFlow;
using Anchor.UI.Panel;
using Cinemachine;
using TMPro;
using DG.Tweening;

namespace Anchor.Trailer
{
    /// <summary>
    /// 宣传片导演脚本 - 控制整个宣传视频的流程、镜头、UI 动画和时间轴
    /// </summary>
    public class TrailerDirector : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private TrailerCameraController cameraController;

        [Header("Timing")]
        [SerializeField] private float totalDuration = 40f;

        [Header("Title Cards")]
        [SerializeField] private GameObject titleCardRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private CanvasGroup titleCardCanvasGroup;

        [Header("Audio")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioClip trailerBGM;

        [Header("Room Positions")]
        [SerializeField] private Vector3 studioOverviewPos = new Vector3(-7.64f, -2.78f, -10f);
        [SerializeField] private Vector3 codeRoomPos = new Vector3(-8.17f, 0.37f, -10f);
        [SerializeField] private Vector3 artRoomPos = new Vector3(-12.19f, -6.17f, -10f);
        [SerializeField] private Vector3 audioRoomPos = new Vector3(-3.30f, -6.49f, -10f);

        private float elapsed;
        private int currentShot;
        private List<Shot> shots = new List<Shot>();

        private class Shot
        {
            public float startTime;
            public float duration;
            public Action onEnter;
            public Action onUpdate;
            public Action onExit;
            public string name;
        }

        void Start()
        {
            // 初始化摄像机控制器
            if (cameraController == null)
            {
                cameraController = gameObject.AddComponent<TrailerCameraController>();
            }
            cameraController.SetCamera(mainCamera);

            SetupShots();
            StartCoroutine(RunTrailer());
        }

        void SetupShots()
        {
            // Shot 0: 黑屏 → Logo 闪现 (0-2s)
            shots.Add(new Shot
            {
                startTime = 0f,
                duration = 2f,
                name = "Logo Flash",
                onEnter = () => {
                    ShowTitleCard("今日开工", "");
                },
                onExit = () => {
                    HideTitleCard();
                }
            });

            // Shot 1: 工作室全景 - 展示三个房间 (2-7s)
            shots.Add(new Shot
            {
                startTime = 2f,
                duration = 5f,
                name = "Studio Overview",
                onEnter = () => {
                    cameraController.SetPositionImmediate(studioOverviewPos);
                    cameraController.SetOrthoSizeImmediate(8.3f);
                    cameraController.ZoomTo(7f, 5f, Ease.InOutQuad);
                }
            });

            // Shot 2: 程序员房间特写 (7-9.5s)
            shots.Add(new Shot
            {
                startTime = 7f,
                duration = 2.5f,
                name = "Code Room Closeup",
                onEnter = () => {
                    cameraController.MoveAndZoom(codeRoomPos, 3.5f, 0.8f, Ease.OutCubic);
                    ShowTitleCard("程序", "降低Bug");
                },
                onExit = () => {
                    HideTitleCard();
                }
            });

            // Shot 3: 美术房间特写 (9.5-12s)
            shots.Add(new Shot
            {
                startTime = 9.5f,
                duration = 2.5f,
                name = "Art Room Closeup",
                onEnter = () => {
                    cameraController.MoveAndZoom(artRoomPos, 3.5f, 0.8f, Ease.OutCubic);
                    ShowTitleCard("美术", "提升画面");
                },
                onExit = () => {
                    HideTitleCard();
                }
            });

            // Shot 4: 音效房间特写 (12-14.5s)
            shots.Add(new Shot
            {
                startTime = 12f,
                duration = 2.5f,
                name = "Audio Room Closeup",
                onEnter = () => {
                    cameraController.MoveAndZoom(audioRoomPos, 3.5f, 0.8f, Ease.OutCubic);
                    ShowTitleCard("音效", "营造氛围");
                },
                onExit = () => {
                    HideTitleCard();
                }
            });

            // Shot 5: 商店系统 - 快速切换道具卡 (14.5-22s)
            shots.Add(new Shot
            {
                startTime = 14.5f,
                duration = 7.5f,
                name = "Shop Montage",
                onEnter = () => {
                    // 拉回到工作室全景准备展示商店
                    cameraController.MoveAndZoom(studioOverviewPos, 7f, 0.6f);
                    // 打开商店 UI
                    StartCoroutine(DelayedAction(0.8f, TryOpenShopUI));
                }
            });

            // Shot 6: 事件面板 (22-26s)
            shots.Add(new Shot
            {
                startTime = 22f,
                duration = 4f,
                name = "Event Panel",
                onEnter = () => {
                    TryOpenEventUI();
                }
            });

            // Shot 7: 行动消耗和房间互动 (26-30s)
            shots.Add(new Shot
            {
                startTime = 26f,
                duration = 4f,
                name = "Weekly Actions",
                onEnter = () => {
                    cameraController.MoveAndZoom(studioOverviewPos, 6.5f, 1f);
                }
            });

            // Shot 8: 结算界面 (30-35s)
            shots.Add(new Shot
            {
                startTime = 30f,
                duration = 5f,
                name = "Settlement Screen",
                onEnter = () => {
                    TryOpenWeekSummaryUI();
                }
            });

            // Shot 9: 最终片尾字幕 (35-40s)
            shots.Add(new Shot
            {
                startTime = 35f,
                duration = 5f,
                name = "End Credits",
                onEnter = () => {
                    ShowTitleCard("今日开工", "48h Game Jam");
                },
                onExit = () => {
                    // 淡出
                    Debug.Log("[Trailer] End");
                }
            });
        }

        IEnumerator RunTrailer()
        {
            if (bgmSource && trailerBGM)
            {
                bgmSource.clip = trailerBGM;
                bgmSource.Play();
            }

            elapsed = 0f;
            currentShot = 0;

            while (elapsed < totalDuration)
            {
                // 检查是否进入下一个镜头
                if (currentShot < shots.Count && elapsed >= shots[currentShot].startTime)
                {
                    Shot shot = shots[currentShot];
                    Debug.Log($"[Trailer] Shot {currentShot}: {shot.name} @ {elapsed:F2}s");
                    shot.onEnter?.Invoke();

                    // 执行镜头
                    float shotElapsed = 0f;
                    while (shotElapsed < shot.duration && elapsed < totalDuration)
                    {
                        shot.onUpdate?.Invoke();
                        yield return null;
                        shotElapsed += Time.deltaTime;
                        elapsed += Time.deltaTime;
                    }

                    shot.onExit?.Invoke();
                    currentShot++;
                }
                else
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }

            Debug.Log("[Trailer] Complete");
        }

        void SetCameraPosition(Vector3 pos)
        {
            if (cameraController != null)
            {
                cameraController.SetPositionImmediate(pos);
            }
        }

        void SetCameraOrthoSize(float size)
        {
            if (cameraController != null)
            {
                cameraController.SetOrthoSizeImmediate(size);
            }
        }

        IEnumerator DelayedAction(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        void ShowTitleCard(string title, string subtitle)
        {
            if (titleCardRoot) titleCardRoot.SetActive(true);
            if (titleText) titleText.text = title;
            if (subtitleText) subtitleText.text = subtitle;
            if (titleCardCanvasGroup)
            {
                titleCardCanvasGroup.alpha = 0f;
                StartCoroutine(FadeCanvasGroup(titleCardCanvasGroup, 0f, 1f, 0.3f));
            }
        }

        void HideTitleCard()
        {
            if (titleCardCanvasGroup && titleCardRoot)
            {
                StartCoroutine(FadeCanvasGroup(titleCardCanvasGroup, 1f, 0f, 0.2f, () => {
                    titleCardRoot.SetActive(false);
                }));
            }
        }

        IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, Action onComplete = null)
        {
            float t = 0f;
            while (t < duration)
            {
                cg.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
                t += Time.deltaTime;
            }
            cg.alpha = to;
            onComplete?.Invoke();
        }

        void TryOpenShopUI()
        {
            var shopPanel = WindowShopPanelManager.Instance;
            if (shopPanel != null)
            {
                // shopPanel.Open();
                Debug.Log("[Trailer] Shop UI opened");
            }
        }

        void TryOpenEventUI()
        {
            var eventPanel = EventPanelManager.Instance;
            if (eventPanel != null)
            {
                // eventPanel.TryShowCurrentEvent();
                Debug.Log("[Trailer] Event UI opened");
            }
        }

        void TryOpenWeekSummaryUI()
        {
            var summaryPanel = WeekPanelManager.Instance;
            if (summaryPanel != null)
            {
                // summaryPanel.Open();
                Debug.Log("[Trailer] Week summary opened");
            }
        }
    }
}
