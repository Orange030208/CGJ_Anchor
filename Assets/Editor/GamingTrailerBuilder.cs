using System;
using System.IO;
using System.Linq;
using Anchor.GameFlow;
using Anchor.Trailer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GamingTrailerBuilder
{
    public const string ScenePath = "Assets/Scenes/Gaming1_Trailer.unity";

    [MenuItem("Tools/Trailer/Build Gaming1 Trailer")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop playback before rebuilding the trailer.");
        var active = SceneManager.GetActiveScene();
        if (active.path == "Assets/Scenes/Gaming1.unity")
            EditorSceneManager.SaveScene(active, ScenePath, true);
        else if (!File.Exists(ScenePath))
            AssetDatabase.CopyAsset("Assets/Scenes/Gaming1.unity",ScenePath);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var old = UnityEngine.Object.FindObjectOfType<GamingTrailerDirector>();
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var director = new GameObject("TRAILER - 77s - Play to watch").AddComponent<GamingTrailerDirector>();
        director.sourceCamera = Camera.main;
        director.sourceCanvas = scene.GetRootGameObjects().First(g=>g.name=="Canvas").GetComponent<Canvas>();
        director.runner = UnityEngine.Object.FindObjectOfType<GameFlowRunner>();
        director.codeRoom = GameObject.Find("Level/Rooms/RoomCode").transform;
        director.artRoom = GameObject.Find("Level/Rooms/RoomArt").transform;
        director.audioRoom = GameObject.Find("Level/Rooms/RoomAudio").transform;
        director.captionFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/fusion-pixel-10px-monospaced-zh_hans.otf");
        director.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ArtRes/后.mp3");
        director.selectSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ArtRes/Audio/选项选择.wav");
        director.purchaseSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ArtRes/Audio/收银机.mp3");
        director.accentSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ArtRes/Audio/盖章.mp3");
        var begin=director.sourceCanvas.GetComponentsInChildren<RectTransform>(true).First(t=>t.name=="BeginPanel");
        director.titleBackgroundSource=(RectTransform)begin.Find("Bg");
        director.titleStartSource=(RectTransform)begin.Find("Start");
        director.pageTurnShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Trailer/TrailerPageTurn.shader");
        director.titleGlowShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Trailer/TrailerTitleGlow.shader");
        director.cloudMistShader=null;
        director.flightMotionShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Trailer/TrailerVerticalMotionBlur.shader");
        director.openingLogo=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Logo.png");
        var ending=director.sourceCanvas.GetComponentInChildren<Anchor.UI.Panel.EndPanelAnimator>(true);
        var endingData=new SerializedObject(ending);
        string[] endingIds={"storm","buggy","boutique","hit"};
        director.endingArtwork=new TrailerEndingGallery.Artwork[4];
        for(int i=0;i<4;i++)
        {
            int slot=3-i;
            director.endingArtwork[i]=new TrailerEndingGallery.Artwork
            {
                id=endingIds[i],
                background=(Sprite)endingData.FindProperty("resultBgSprites").GetArrayElementAtIndex(slot).objectReferenceValue,
                result=(Sprite)endingData.FindProperty("resultSprites").GetArrayElementAtIndex(slot).objectReferenceValue,
                character=(Sprite)endingData.FindProperty("idleSprites").GetArrayElementAtIndex(slot).objectReferenceValue
            };
        }
        var settings = new SerializedObject(director.runner);
        settings.FindProperty("mStartOnAwake").boolValue = false;
        settings.FindProperty("mLogEvents").boolValue = false;
        settings.ApplyModifiedPropertiesWithoutUndo();
        foreach(Transform child in director.sourceCanvas.transform) child.gameObject.SetActive(false);
        var bgm = director.sourceCanvas.GetComponent<Anchor.Audio.MenuGameBgmPlayer>();
        if(bgm != null) bgm.enabled=false;
        // Repair the missing price binding on a trailer-only copy of the existing Buff prefab.
        const string cardPath="Assets/Trailer/BuffCard_Trailer.prefab";
        Directory.CreateDirectory("Assets/Trailer");
        AssetDatabase.Refresh();
        if(!File.Exists(cardPath)) AssetDatabase.CopyAsset("Assets/Prefabs/WIndows/Buff.prefab",cardPath);
        var cardRoot=PrefabUtility.LoadPrefabContents(cardPath);
        var card=cardRoot.GetComponent<BuffCardController>();
        var cardData=new SerializedObject(card);
        cardData.FindProperty("costText").objectReferenceValue=cardRoot.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        cardData.FindProperty("iconImage").objectReferenceValue=cardRoot.GetComponent<Image>();
        cardData.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(cardRoot,cardPath);
        PrefabUtility.UnloadPrefabContents(cardRoot);
        var shop=director.sourceCanvas.GetComponentInChildren<Anchor.UI.Panel.WindowShopPanelManager>(true);
        var shopData=new SerializedObject(shop);
        shopData.FindProperty("buffCardPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(cardPath).GetComponent<BuffCardController>();
        shopData.ApplyModifiedPropertiesWithoutUndo();
        foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
            if(canvas.renderMode==RenderMode.WorldSpace) { canvas.overrideSorting=true; canvas.sortingOrder=21000; }
        var hud=director.sourceCanvas.GetComponentInChildren<Anchor.UI.Panel.MainPanelManager>(true);
        foreach(var r in hud.GetComponentsInChildren<RectTransform>(true))
        {
            if(r.name=="Dollar") r.anchoredPosition=new Vector2(r.anchoredPosition.x,290);
            if(r.name=="NextWeek") r.anchoredPosition=new Vector2(r.anchoredPosition.x,280);
        }
        var center = (Vector2)GameObject.Find("Level/Virtual Camera").transform.position;
        center.y -= .5f;
        var code = (Vector2)director.codeRoom.position;
        var art = (Vector2)director.artRoom.position;
        var audio = (Vector2)director.audioRoom.position;
        director.shots = new[]
        {
            Shot("Original studio Logo",3,"","","",center,8.7f,8.7f),
            Shot("Studio reveal",4,"今日开工  /  A GAME ABOUT MAKING GAMES","一间工作室，无数种可能。","经营 · 决策 · 随机成长",center,8.7f,8.1f),
            Shot("Code room details",2.5f,"01  /  三种专长，一款作品","让想法，开始运转。","程序  /  降低 Bug",code,4.5f,4.1f),
            Shot("Art room details",2.5f,"01  /  三种专长，一款作品","让画面，有自己的性格。","美术  /  提升画面",art,4.7f,4.25f),
            Shot("Audio room details",3,"01  /  三种专长，一款作品","让世界，拥有声音。","音效  /  营造氛围",audio,4.7f,4.25f),
            Shot("Five shop pages",7,"02  /  有限预算，无限搭配","把预算，花在刀刃上。","五组道具，快速构筑你的开局",center,8.31f,8.31f),
            Shot("Purchase closeup",3,"02  /  有限预算，无限搭配","这笔投入，会改变什么？","购买 · 成长 · 构筑",center,8.31f,8.31f,.86f,new Vector2(.5f,.55f)),
            Shot("Weekly event",4,"03  /  每一周，都有意外","计划之外，才是日常。","随机事件，把决定交给你",center,8.31f,8.31f),
            Shot("Decision closeup",3,"03  /  每一周，都有意外","机遇，还是代价？","选择之后，继续向前",center,8.31f,8.31f,.9f,new Vector2(.53f,.57f)),
            Shot("Program action and flame",4,"04  /  每一点行动，都有分量","先修 Bug，还是先做内容？","投入行动点，看见即时变化",code+new Vector2(2.5f,-.3f),6.3f,6.1f),
            Shot("Art action",4,"04  /  每一点行动，都有分量","好画面，也需要取舍。","程序 · 美术 · 音效，共享有限行动点",art+new Vector2(2.5f,.3f),6.3f,6.1f),
            Shot("Audio action",4,"04  /  每一点行动，都有分量","为氛围，多投入一点。","1 AP 还是 2 AP，权衡每一周的投入",audio+new Vector2(2.5f,.3f),6.3f,6.1f),
            Shot("Weekly settlement animation",5,"05  /  让努力，变成成绩","这一周，交出答卷。","结算动画 · 品质变化 · 愿望单增长",center,8.31f,8.31f),
            Shot("Score closeup",3,"05  /  让努力，变成成绩","向更高的分数，再近一步。","每次选择，累积成不同的结果",center,8.31f,8.31f,.85f,new Vector2(.5f,.5f)),
            Shot("Ending 1 - storm",2.4f,"06  /  不同成绩，不同结局","每一次开工，都有不同的终点。","结局 01 / 04  ·  暴死",center,8.31f,8.31f),
            Shot("Ending 2 - buggy",2.4f,"06  /  不同成绩，不同结局","从无人问津，继续向前。","结局 02 / 04  ·  无人问津",center,8.31f,8.31f),
            Shot("Ending 3 - boutique",2.4f,"06  /  不同成绩，不同结局","让作品，找到懂它的人。","结局 03 / 04  ·  小众精品",center,8.31f,8.31f),
            Shot("Ending 4 - hit and pixel fireworks",4.8f,"06  /  不同成绩，不同结局","直到，好评如潮。","结局 04 / 04  ·  向最好的结局出发",center,8.31f,8.31f),
            Shot("Studio farewell and fade to black",2,"今日开工  /  48 小时 GAME JAM 作品","下一款游戏，由你决定。","一只肥鸽  ·  今日开工",center,8.2f,7.8f),
            Shot("Fast nonlinear rise - sparse clouds and motion blur",4,"","","",center,7.8f,9),
            Shot("Original Start breathing hold",7,"","","",center,9,9)
        };
        int[] beats={-1,0,1,2,3,4,5,6,7,8,9,10,11,12,15,16,17,18,14,19,20};
        for(int i=0;i<director.shots.Length;i++) director.shots[i].beat=beats[i];
        director.sourceCamera.transform.position = new Vector3(center.x,center.y,-10.022238f);
        director.sourceCamera.orthographicSize = 8.7f;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=director.gameObject;
        Validate();
        Debug.Log($"[Trailer] Built {ScenePath}: {director.Duration}s / {director.shots.Length} shots. Home={center}, code={code}, art={art}, audio={audio}");
    }

    private static GamingTrailerDirector.Shot Shot(string name,float duration,string chapter,string title,string detail,Vector2 center,float size,float endSize,float crop=1,Vector2? cropCenter=null)
    {
        return new GamingTrailerDirector.Shot { name=name,duration=duration,chapter=chapter,headline=title,detail=detail,center=center,size=size,endSize=endSize,crop=crop,cropCenter=cropCenter??new Vector2(.5f,.5f) };
    }

    [MenuItem("Tools/Trailer/Play and Capture Review")]
    public static void Review()
    {
        SessionState.SetBool("TrailerReview",true);
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.isPlaying=true;
    }

    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    private static void OnPlayMode(PlayModeStateChange change)
    {
        if(change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("TrailerReview",false)) return;
        SessionState.SetBool("TrailerReview",false);
        var director=UnityEngine.Object.FindObjectOfType<GamingTrailerDirector>();
        if(director!=null) director.captureReviewFrames=true;
    }

    [MenuItem("Tools/Trailer/Write Runtime Report")]
    public static void Report()
    {
        var director=UnityEngine.Object.FindObjectOfType<GamingTrailerDirector>();
        var report=new System.Text.StringBuilder();
        if(director!=null) report.AppendLine($"Elapsed={director.Elapsed}; Duration={director.Duration}; Finished={director.Finished}; Shot={director.ShotIndex}");
        if(director!=null && director.runner.Controller!=null)
        {
            var b=director.runner.Controller.Blackboard;
            report.AppendLine($"State={director.runner.Controller.CurrentState}; month={b.MonthIndex}; week={b.WeekIndex}; AP={b.RemainingActionPoints}; visual={b.VisualScore}; audio={b.AtmosphereScore}; bug={b.BugScore}; quality={b.QualityScore}; wish={b.WishlistCount}; buffs={b.ActiveBuffIds.Count}; coins={b.Coins}");
        }
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(t.GetComponent<Canvas>()!=null || t.GetComponent<FloatingUIManager>()!=null || t.GetComponent<ButtonPrefabSpawner>()!=null || t.GetComponent<Camera>()!=null)
                    report.AppendLine($"{PathOf(t)} active={t.gameObject.activeInHierarchy} position={t.position} scale={t.lossyScale} components={string.Join(",",t.GetComponents<Component>().Select(c=>c==null?"MISSING":c.GetType().Name))}");
        Directory.CreateDirectory("outputs/trailer-review");
        File.WriteAllText("outputs/trailer-review/runtime.txt",report.ToString());
    }
    private static string PathOf(Transform t) => t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;

    [MenuItem("Tools/Trailer/Validate Trailer Scene")]
    public static void Validate()
    {
        var director=UnityEngine.Object.FindObjectOfType<GamingTrailerDirector>();
        if(director==null || SceneManager.GetActiveScene().path!=ScenePath) throw new InvalidOperationException("Open Gaming1_Trailer first.");
        if(director.Duration<=30 || director.shots.Length!=21) throw new InvalidOperationException("Trailer must contain twenty-one shots and run longer than thirty seconds.");
        if(director.sourceCamera==null || director.sourceCanvas==null || director.runner==null || director.captionFont==null || director.music==null || director.selectSound==null || director.purchaseSound==null || director.accentSound==null)
            throw new InvalidOperationException("A required scene or audio reference is missing.");
        if(director.titleBackgroundSource==null || director.titleStartSource==null || director.pageTurnShader==null || director.titleGlowShader==null || director.flightMotionShader==null || director.openingLogo==null)
            throw new InvalidOperationException("A title node or trailer shader reference is missing.");
        if(director.endingArtwork==null || director.endingArtwork.Length!=4 || director.endingArtwork.Any(e=>e.background==null || e.result==null || e.character==null))
            throw new InvalidOperationException("One of the twelve original ending artwork references is missing.");
        if(ShaderUtil.ShaderHasError(director.pageTurnShader) || ShaderUtil.ShaderHasError(director.titleGlowShader) || ShaderUtil.ShaderHasError(director.flightMotionShader))
            throw new InvalidOperationException("A trailer shader failed to compile.");
        int missing=0;
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
        if(missing!=0) throw new InvalidOperationException($"Missing components: {missing}");
        Directory.CreateDirectory("outputs/trailer-review");
        File.WriteAllText("outputs/trailer-review/scene-validation.txt",$"PASS\nScene: {ScenePath}\nDuration: {director.Duration}s\nShots: {director.shots.Length}\nMissing scripts: {missing}\nRequired references: all present\nSource: Gaming1 scene, project artwork, original gameplay panels and audio\n");
        Debug.Log($"[Trailer] Validation PASS: {director.Duration}s, {director.shots.Length} shots, original Bg/Start, shaders, all required references, no missing scripts.");
    }
}
