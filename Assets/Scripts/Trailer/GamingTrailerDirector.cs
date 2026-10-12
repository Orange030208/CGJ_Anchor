using System;
using System.Collections.Generic;
using Anchor.GameFlow;
using Anchor.UI;
using Anchor.UI.Panel;
using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Anchor.Trailer
{
    /// <summary>Plays an edited, live-rendered tour of Gaming1. No video or external artwork is used.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class GamingTrailerDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class Shot
        {
            public int beat;
            public string name;
            public float duration = 4;
            public string chapter;
            public string headline;
            public string detail;
            public Vector2 center;
            public float size = 8.31f;
            public float endSize = 8;
            public Vector2 cropCenter = new Vector2(.5f, .5f);
            public float crop = 1;
        }

        [Header("Live source from Gaming1")]
        public Camera sourceCamera;
        public Canvas sourceCanvas;
        public GameFlowRunner runner;
        public Transform codeRoom;
        public Transform artRoom;
        public Transform audioRoom;
        public Font captionFont;
        public AudioClip music;
        public AudioClip selectSound;
        public AudioClip purchaseSound;
        public AudioClip accentSound;
        public RectTransform titleBackgroundSource;
        public RectTransform titleStartSource;
        public Shader pageTurnShader;
        public Shader titleGlowShader;
        [HideInInspector] public Shader cloudMistShader; // Previous revision's serialized field.
        public Shader flightMotionShader;
        public Sprite openingLogo;
        public TrailerEndingGallery.Artwork[] endingArtwork;
        [Header("Edit (seconds)")]
        public Shot[] shots;
        [NonSerialized] public bool captureReviewFrames;
        public float Elapsed { get; private set; }
        public int ShotIndex { get; private set; } = -1;
        public bool Finished { get; private set; }
        private int Beat => ShotIndex<0 ? -1 : shots[ShotIndex].beat;
        public float Duration { get { float d = 0; foreach (var s in shots) d += s.duration; return d; } }

        private RenderTexture frame;
        private RawImage screen;
        private RectTransform film;
        private CanvasGroup titles;
        private Text chapterText, headlineText, detailText;
        private Image progress;
        private AudioSource musicSource, effects;
        private float shotStart;
        private float previousTime;
        private bool initialized;
        private readonly HashSet<int> cues = new HashSet<int>();
        private bool previousCursor;
        private int previousFrameRate;
        private bool previousRunInBackground;
        private FloatingUIManager activeFan;
        private int reviewIndex;
        private bool reviewCapturedThisFrame;
        private TrailerPresentation presentation;
        private TrailerEndingGallery endingGallery;
        private readonly List<Anchor.Config.game.buff> shopOffers = new List<Anchor.Config.game.buff>();
        private readonly List<Anchor.Config.game.buff> shopPage = new List<Anchor.Config.game.buff>();

        private void Awake()
        {
            // Re-time the same Logo inside the film so it is included in captures and duration.
            foreach (var splash in FindObjectsOfType<StartupLogoSplash>(true))
            {
                splash.gameObject.SetActive(false);
                Destroy(splash.gameObject);
            }
            previousCursor = Cursor.visible;
            previousFrameRate = Application.targetFrameRate;
            previousRunInBackground = Application.runInBackground;
            Cursor.visible = false;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            foreach (var brain in FindObjectsOfType<CinemachineBrain>()) brain.enabled = false;
            foreach (var zoom in FindObjectsOfType<CinemachineScrollZoomController>()) zoom.enabled = false;
            foreach (var follow in FindObjectsOfType<MouseWorldFollower>()) follow.enabled = false;
            foreach (var input in FindObjectsOfType<EventSystem>()) input.enabled = false;
            foreach (var bgm in FindObjectsOfType<Anchor.Audio.MenuGameBgmPlayer>()) bgm.enabled = false;
            foreach (var canvas in FindObjectsOfType<Canvas>(true))
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = sourceCamera;
                canvas.planeDistance = 2;
            }
            HidePanels();
            BuildScreen();
            SetCamera(shots[0], 0);
        }

        private void Start()
        {
            runner.StartNewGame();
            // Establish a real second-month run so settlement can show accumulated growth.
            for (int week = 0; week < 4; week++)
            {
                if (runner.Controller.CurrentState == GameFlowState.BudgetShop) runner.ConfirmBudgetShop();
                ResolveRemainingEvents();
                runner.TrySpendProgramOneActionPoint();
                runner.TrySpendArtOneActionPoint();
                runner.TrySpendAudioOneActionPoint();
                runner.FinishWeekAction();
                runner.ContinueFlow();
            }
            if (runner.Controller.CurrentState == GameFlowState.MonthSettlement) runner.ContinueFlow();
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = music;
            musicSource.loop = true;
            musicSource.volume = 0;
            musicSource.Play();
            effects = gameObject.AddComponent<AudioSource>();
            initialized = true;
            previousTime = Time.realtimeSinceStartup;
            EnterShot(0);
            Debug.Log($"[Trailer] Started {Duration:0.0}s live Gaming1 film.");
        }

        private void Update()
        {
            if (!initialized || Finished) return;
            float now = Time.realtimeSinceStartup;
            Elapsed += Mathf.Min(now - previousTime, .1f);
            previousTime = now;
            if (Elapsed >= Duration)
            {
                Elapsed = Duration;
                Finished = true;
                musicSource.volume = 0;
                Debug.Log($"[Trailer] Completed {Duration:0.0}s. State={runner.Controller.CurrentState}; AP={runner.Controller.Blackboard.RemainingActionPoints}; Wishlist={runner.Controller.Blackboard.WishlistCount}");
                return;
            }
            while (ShotIndex + 1 < shots.Length && Elapsed >= shotStart + shots[ShotIndex].duration)
            {
                shotStart += shots[ShotIndex].duration;
                EnterShot(ShotIndex + 1);
            }
            float local = Elapsed - shotStart;
            Shot shot = shots[ShotIndex];
            float u = Mathf.Clamp01(local / shot.duration);
            SetCamera(shot, u);
            titles.alpha = Mathf.SmoothStep(0, 1, local / .5f) * (ShotIndex == shots.Length-1 ? 1 : Mathf.Clamp01((shot.duration-local)/.3f));
            headlineText.rectTransform.anchoredPosition = new Vector2(84 + 16 * (1 - Mathf.SmoothStep(0,1,local/.6f)), 110);
            progress.rectTransform.anchorMax = new Vector2(Elapsed / Duration, 0);
            musicSource.volume = .42f * Mathf.Clamp01(Elapsed/1.2f) * Mathf.Clamp01((Duration-Elapsed)/2);
            reviewCapturedThisFrame=false;
            RunCues(local);
            presentation.TickPage(Elapsed);
            if (Beat == -1) presentation.TickLogo(local,shot.duration);
            if (Beat == 14) presentation.DimStudio(Mathf.SmoothStep(0,1,(local-1.15f)/.85f));
            if (Beat >= 15 && Beat <= 18) endingGallery.Tick(local,shot.duration);
            if (Beat >= 19) presentation.TickFlight(Beat == 19 ? local : 4+local);
            if (captureReviewFrames && !reviewCapturedThisFrame && Elapsed >= 1 + reviewIndex * 2)
            {
                string folder = System.IO.Path.Combine(Application.dataPath, "../outputs/trailer-review");
                System.IO.Directory.CreateDirectory(folder);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, $"frame-{reviewIndex:00}-{ShotIndex:00}.png"));
                reviewIndex++;
            }
        }

        private void LateUpdate()
        {
            if (film != null)
            {
                var parent = (RectTransform)film.parent;
                film.localScale = Vector3.one * Mathf.Min(parent.rect.width / 1920f, parent.rect.height / 1080f);
            }
            // YokiFrame may assign the root canvas sorting order during its own startup.
            sourceCanvas.sortingOrder = 20000;
            if (initialized) SetCamera(shots[Mathf.Max(0,ShotIndex)], Mathf.Clamp01((Elapsed-shotStart)/shots[Mathf.Max(0,ShotIndex)].duration));
        }

        private void SetCamera(Shot shot, float u)
        {
            float eased = u*u*(3-2*u);
            sourceCamera.transform.position = new Vector3(shot.center.x, shot.center.y, -10.022238f);
            if (Beat >= 19)
                sourceCamera.transform.position += Vector3.up * (40 * TrailerPresentation.FlightProgress(Beat==19 ? Elapsed-shotStart : 4));
            sourceCamera.transform.rotation = Quaternion.identity;
            sourceCamera.orthographicSize = Mathf.Lerp(shot.size, shot.endSize, eased);
            float crop = Mathf.Lerp(shot.crop, shot.crop * .975f, eased);
            screen.uvRect = new Rect(Mathf.Clamp(shot.cropCenter.x-crop/2,0,1-crop), Mathf.Clamp(shot.cropCenter.y-crop/2,0,1-crop),crop,crop);
        }

        private void EnterShot(int index)
        {
            ShotIndex = index;
            cues.Clear();
            Shot s = shots[index];
            chapterText.text = s.chapter;
            headlineText.text = s.headline;
            detailText.text = s.detail;
            titles.alpha = 0;
            SetCamera(s,0);
            bool captionsVisible=Beat>=0 && Beat<19;
            film.Find("Caption shade").gameObject.SetActive(captionsVisible);
            film.Find("Chapter shade").gameObject.SetActive(captionsVisible);
            titles.gameObject.SetActive(captionsVisible);
            progress.gameObject.SetActive(captionsVisible);
            if (Beat > 0 && Beat != 5 && Beat != 7 && Beat != 12) effects.PlayOneShot(selectSound, .22f);
            switch (Beat)
            {
                case -1: HidePanels(); break;
                case 0: presentation.HideLogo(); HidePanels(); break;
                case 4:
                    HidePanels();
                    WindowShopPanelManager.Instance.Open();
                    break;
                case 6:
                    WindowShopPanelManager.Instance.Close();
                    PrepareEvent();
                    EventPanelManager.Instance.TryShowCurrentEvent();
                    break;
                case 8:
                    EventPanelManager.Instance.Close();
                    ResolveRemainingEvents();
                    MainPanelManager.Instance.Open();
                    OpenRoom(codeRoom);
                    break;
                case 9: CloseRoom(); OpenRoom(artRoom); break;
                case 10: CloseRoom(); OpenRoom(audioRoom); break;
                case 11:
                    CloseRoom();
                    runner.FinishWeekAction();
                    MainPanelManager.Instance.Close();
                    WeekPanelManager.Instance.Open();
                    break;
                case 13:
                    WeekPanelManager.Instance.Close();
                    runner.ContinueFlow();
                    ResolveRemainingEvents();
                    MainPanelManager.Instance.Open();
                    break;
                case 14:
                    endingGallery.Hide();
                    HidePanels();
                    break;
                case 15:
                case 16:
                case 17:
                case 18:
                    presentation.TurnPage(frame,screen.uvRect,Elapsed,.55f);
                    HidePanels();
                    endingGallery.Show(Beat-15);
                    break;
                case 19:
                    HidePanels();
                    endingGallery.Hide();
                    presentation.BeginFlight();
                    break;
            }
            Debug.Log($"[Trailer] Shot {index+1:00}/{shots.Length}: {s.name} @ {Elapsed:0.00}s");
        }

        private bool Cue(int id, float time, float now) => now >= time && cues.Add(id);

        private void RunCues(float t)
        {
            if (Beat == 4 && Cue(0,.65f,t))
            {
                // One real offer pool keeps every displayed item purchasable across the five pages.
                shopOffers.AddRange(runner.RefreshBudgetShopBuffOffers(15));
                ShowShopPage(0);
            }
            if (Beat == 4)
                for (int page=1;page<5;page++)
                    if (Cue(page,.65f + page + (page-1)*TrailerPresentation.PageTurnDuration,t))
                    {
                        presentation.TurnPage(frame,screen.uvRect,Elapsed);
                        ShowShopPage(page);
                        effects.PlayOneShot(selectSound,.4f);
                    }
            if (Beat == 5 && Cue(0,1.1f,t))
            {
                // Use the card's existing purchase callbacks, including money and inventory animation.
                var introduction = WindowShopPanelManager.Instance.GetComponentInChildren<IntroductionWindowController>(true);
                if (introduction != null)
                {
                    foreach(var button in introduction.GetComponentsInChildren<Button>())
                        if(button.name=="Buy") { button.onClick.Invoke(); break; }
                }
                Debug.Log($"[Trailer] Purchase: buffs={runner.Controller.Blackboard.ActiveBuffIds.Count}; coins={runner.Controller.Blackboard.Coins}");
            }
            if (Beat == 7 && Cue(0,2.0f,t))
            {
                string choice=PreferEventYes()?"Yes":"No";
                foreach (var button in EventPanelManager.Instance.GetComponentsInChildren<Button>())
                    if (button.name.IndexOf(choice,StringComparison.OrdinalIgnoreCase)>=0) { button.onClick.Invoke(); break; }
                effects.PlayOneShot(selectSound,.6f);
            }
            if ((Beat == 8 || Beat == 9 || Beat == 10) && Cue(0,2.2f,t))
            {
                if (activeFan != null)
                {
                    var buttons = activeFan.GetComponentsInChildren<Button>();
                    int selected = Beat == 10 && runner.Controller.Blackboard.RemainingActionPoints >= 2 ? 1 : 0;
                    if (buttons.Length > selected) buttons[selected].onClick.Invoke();
                    Debug.Log($"[Trailer] Action: AP={runner.Controller.Blackboard.RemainingActionPoints}; visual={runner.Controller.Blackboard.VisualScore}; audio={runner.Controller.Blackboard.AtmosphereScore}; wish={runner.Controller.Blackboard.WishlistCount}");
                }
            }
            if ((Beat == 8 || Beat == 9 || Beat == 10) && Cue(1,1.1f,t) && activeFan!=null)
            {
                foreach(var flame in activeFan.GetComponentsInChildren<ScreenSpaceEdgeFlame>()) flame.SetHoverState(true);
                var buttons=activeFan.GetComponentsInChildren<Button>();
                int hovered=Beat==10 && buttons.Length>1?1:0;
                if(buttons.Length>hovered) ExecuteEvents.Execute(buttons[hovered].gameObject,new PointerEventData(null),ExecuteEvents.pointerEnterHandler);
            }
            if (captureReviewFrames && (Beat==8 || Beat==9 || Beat==10) && Cue(2,1.8f,t))
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../outputs/trailer-review",$"flame-{ShotIndex}.png"));
            if (Beat == 12 && Cue(0,.2f,t)) effects.PlayOneShot(accentSound,.4f);
            if(captureReviewFrames && Beat==4)
                for(int page=0;page<5;page++)
                {
                    if(Cue(100+page,.95f+page*1.32f,t)) CaptureReview($"shop-page-{page+1}.png");
                    if(page<4 && Cue(110+page,1.79f+page*1.32f,t)) CaptureReview($"shop-turn-{page+1}.png");
                }
            if(Beat==18 && Cue(5,1.45f,t)) effects.PlayOneShot(accentSound,.65f);
            if(captureReviewFrames && Beat==-1 && Cue(100,1.5f,t)) CaptureReview("opening-logo.png");
            if(captureReviewFrames && Beat>=15 && Beat<=18)
            {
                if(Cue(100,1.0f,t)) CaptureReview($"ending-{Beat-15}.png");
                if(Cue(101,.22f,t)) CaptureReview($"ending-turn-{Beat-15}.png");
                if(Beat==18)
                    for(int i=0;i<7;i++)
                        if(Cue(110+i,1.4f+i*.4f,t)) CaptureReview($"ending-best-{i}.png");
            }
            if(captureReviewFrames && Beat==19)
                for(int i=0;i<8;i++)
                    if(Cue(100+i,.2f+i*.5f,t)) CaptureReview($"flight-{i}.png");
            if(captureReviewFrames && Beat==20)
                for(int i=0;i<4;i++)
                    if(Cue(100+i,.3f+i*.6f,t)) CaptureReview($"title-glow-{i}.png");
        }

        private void CaptureReview(string filename)
        {
            reviewCapturedThisFrame=true;
            string folder=System.IO.Path.Combine(Application.dataPath,"../outputs/trailer-review");
            System.IO.Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,filename));
        }

        private void ShowShopPage(int page)
        {
            shopPage.Clear();
            for (int i=0;i<3 && shopOffers.Count>0;i++) shopPage.Add(shopOffers[(page*3+i)%shopOffers.Count]);
            if (shopPage.Count==0) return;
            var shop=WindowShopPanelManager.Instance;
            shop.InjectData(shopPage);
            var featured=shopPage[page%shopPage.Count];
            // On the last page select an affordable offer for the following live purchase.
            if (page==4)
                foreach(var offer in shopPage)
                    if(runner.CanPurchaseBudgetShopBuff(offer.Id)) { featured=offer; break; }
            shop.OpenIntroductionWindow(featured);
            detailText.text=$"道具 {page+1:00} / 05  ·  {featured.Title}  ·  {featured.Content.Replace("\n"," ").Replace("\r","")}";
            Debug.Log($"[Trailer] Shop page {page+1}/5 @ {Elapsed:0.00}s: {string.Join(", ",shopPage.ConvertAll(b=>b.Title))}; effect={featured.Content}");
        }

        private void PrepareEvent()
        {
            if (runner.Controller.CurrentState == GameFlowState.BudgetShop) runner.ConfirmBudgetShop();
            // Advance only real flow states until an eligible weekly event is reached.
            for (int i=0;i<16 && runner.Controller.CurrentState != GameFlowState.WeekEvent;i++)
            {
                switch (runner.Controller.CurrentState)
                {
                    case GameFlowState.WeekAction: runner.FinishWeekAction(); break;
                    case GameFlowState.WeekResolve:
                    case GameFlowState.MonthSettlement: runner.ContinueFlow(); break;
                    case GameFlowState.BudgetShop: runner.ConfirmBudgetShop(); break;
                    default: return;
                }
            }
        }

        private void ResolveRemainingEvents()
        {
            for (int i=0;i<8 && runner.Controller.CurrentState==GameFlowState.WeekEvent;i++)
                if(PreferEventYes()) runner.ChooseWeekGameEventYes(); else runner.ChooseWeekGameEventNo();
        }

        private bool PreferEventYes()
        {
            var current=runner.CurrentWeekGameEvent;
            // Choose a real event branch that leaves room for the three subsequent action demonstrations.
            return current!=null && ActionPointDelta(current.YesEffects)>=ActionPointDelta(current.NoEffects);
        }

        private static int ActionPointDelta(int[][] changes)
        {
            int value=0;
            foreach(var change in changes)
                if(change.Length>=2 && (change[0]==1001 || change[0]==1002)) value+=change[1];
            return value;
        }

        private void OpenRoom(Transform room)
        {
            activeFan = room.GetComponentInChildren<FloatingUIManager>(true);
            if(activeFan!=null) FrameFan(activeFan);
            var spawner = room.GetComponentInChildren<ButtonPrefabSpawner>(true);
            if (spawner != null) spawner.OpenTargetObject();
            if (activeFan != null)
            {
                activeFan.gameObject.SetActive(true);
                activeFan.OpenFan();
            }
        }

        private void FrameFan(FloatingUIManager fan)
        {
            // Reframe the existing world-space card fan into the right half of the closeup.
            // The authored card positions and its own fan/flame animations are retained.
            var buttons=fan.GetComponentsInChildren<Button>(true);
            if(buttons.Length==0) return;
            var corners=new Vector3[4];
            Bounds bounds=new Bounds(buttons[0].transform.position,Vector3.zero);
            foreach(var b in buttons)
            {
                ((RectTransform)b.transform).GetWorldCorners(corners);
                foreach(var c in corners) bounds.Encapsulate(c);
            }
            float factor=Mathf.Min(sourceCamera.orthographicSize*2*sourceCamera.aspect*.46f/Mathf.Max(.01f,bounds.size.x),sourceCamera.orthographicSize*2*.57f/Mathf.Max(.01f,bounds.size.y));
            Vector3 original=fan.transform.position;
            fan.transform.localScale*=factor;
            Vector3 scaledCenter=original+(bounds.center-original)*factor;
            Vector3 target=sourceCamera.ViewportToWorldPoint(new Vector3(.68f,.56f,10));
            target.z=scaledCenter.z;
            fan.transform.position+=target-scaledCenter;
        }

        private void CloseRoom()
        {
            if (activeFan == null) return;
            if (activeFan.Spawner != null) activeFan.Spawner.CloseTargetObject();
            else activeFan.CloseFan();
            activeFan = null;
        }

        private void HidePanels()
        {
            foreach (Transform child in sourceCanvas.transform) child.gameObject.SetActive(false);
        }

        private void BuildScreen()
        {
            frame = new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32) { name="Trailer live 1080p", filterMode=FilterMode.Bilinear };
            frame.Create();
            sourceCamera.targetTexture = frame;
            sourceCamera.aspect = 16f/9;
            var outputCamera = new GameObject("Trailer output camera",typeof(Camera)).GetComponent<Camera>();
            outputCamera.transform.SetParent(transform);
            outputCamera.cullingMask = 0;
            outputCamera.clearFlags = CameraClearFlags.SolidColor;
            outputCamera.backgroundColor = new Color(.025f,.035f,.055f);
            outputCamera.depth = 20;
            var canvas = new GameObject("Trailer film",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform,false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080);
            scaler.matchWidthOrHeight = .5f;
            film = new GameObject("16x9 safe frame",typeof(RectTransform)).GetComponent<RectTransform>();
            film.SetParent(canvas.transform,false);
            film.anchorMin = film.anchorMax = new Vector2(.5f,.5f);
            film.sizeDelta = new Vector2(1920,1080);
            film.gameObject.AddComponent<RectMask2D>();
            screen = new GameObject("Live Gaming1",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
            screen.transform.SetParent(film,false);
            Stretch(screen.rectTransform);
            screen.texture = frame;
            screen.raycastTarget = false;
            presentation = gameObject.AddComponent<TrailerPresentation>();
            presentation.Build(film,titleBackgroundSource,titleStartSource,pageTurnShader,titleGlowShader);
            presentation.ConfigureOpeningAndClouds(film,openingLogo,flightMotionShader);
            endingGallery=gameObject.AddComponent<TrailerEndingGallery>();
            endingGallery.Build(sourceCanvas,endingArtwork);
            // Narrow translucent edges keep the game visible throughout every title and cut.
            MakeBand("Caption shade",0,210,new Color(.025f,.035f,.055f,.64f));
            MakeBand("Chapter shade",1,82,new Color(.025f,.035f,.055f,.50f));
            var textRoot = new GameObject("Captions",typeof(RectTransform),typeof(CanvasGroup));
            textRoot.transform.SetParent(film,false);
            Stretch(textRoot.GetComponent<RectTransform>());
            titles = textRoot.GetComponent<CanvasGroup>();
            titles.blocksRaycasts = false;
            chapterText = MakeText(textRoot.transform,"Chapter",24,new Vector2(84,-40),new Vector2(1650,48),true,new Color(.42f,.96f,.87f));
            headlineText = MakeText(textRoot.transform,"Headline",50,new Vector2(84,110),new Vector2(1750,72),false,Color.white);
            detailText = MakeText(textRoot.transform,"Detail",25,new Vector2(88,58),new Vector2(1700,44),false,new Color(.71f,.83f,.87f));
            progress = new GameObject("Film progress",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            progress.transform.SetParent(film,false);
            progress.rectTransform.anchorMin = new Vector2(0,0);
            progress.rectTransform.anchorMax = new Vector2(1,0);
            progress.rectTransform.pivot = new Vector2(.5f,0);
            progress.rectTransform.sizeDelta = new Vector2(0,4);
            progress.color = new Color(.42f,.96f,.87f);
            progress.type = Image.Type.Simple;
            progress.raycastTarget = false;
        }

        private void MakeBand(string name,float y,float h,Color color)
        {
            var image = new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(film,false);
            image.rectTransform.anchorMin = new Vector2(0,y);
            image.rectTransform.anchorMax = new Vector2(1,y);
            image.rectTransform.pivot = new Vector2(.5f,y);
            image.rectTransform.sizeDelta = new Vector2(0,h);
            image.color = color;
            image.raycastTarget = false;
        }

        private Text MakeText(Transform parent,string name,int size,Vector2 position,Vector2 dimensions,bool top,Color color)
        {
            var t = new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(parent,false);
            t.font = captionFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleLeft;
            t.raycastTarget = false;
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0,top?1:0);
            t.rectTransform.pivot = new Vector2(0,.5f);
            t.rectTransform.anchoredPosition = position;
            t.rectTransform.sizeDelta = dimensions;
            return t;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            Cursor.visible = previousCursor;
            Application.targetFrameRate = previousFrameRate;
            Application.runInBackground = previousRunInBackground;
            if (sourceCamera != null) sourceCamera.targetTexture = null;
            if (frame != null) { frame.Release(); Destroy(frame); }
        }
    }
}
