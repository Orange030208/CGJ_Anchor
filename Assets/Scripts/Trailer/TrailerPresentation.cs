using UnityEngine;
using UnityEngine.UI;

namespace Anchor.Trailer
{
    /// <summary>Trailer-only page turns and the flight into Gaming1's existing title artwork.</summary>
    public sealed class TrailerPresentation : MonoBehaviour
    {
        public const float PageTurnDuration = .32f;
        private RawImage page;
        private RenderTexture pageSnapshot;
        private Material pageMaterial, glowMaterial, cityMotionMaterial;
        private Image studioDim;
        private CanvasGroup ending, cityOpacity, startOpacity, atmosphere;
        private RectTransform city, start;
        private Image glow;
        private readonly RectTransform[] clouds = new RectTransform[4];
        private readonly TrailerCloudGraphic[] cloudGraphics = new TrailerCloudGraphic[4];
        private readonly RectTransform[] lines = new RectTransform[36];
        private float pageStarted = -100;
        private float pageDuration = PageTurnDuration;
        private bool flying;
        private CanvasGroup logoRoot;
        private Image logoImage;

        public void Build(RectTransform film, RectTransform sourceBg, RectTransform sourceStart, Shader turnShader, Shader glowShader)
        {
            pageSnapshot = new RenderTexture(1920,1080,0,RenderTextureFormat.ARGB32) { name="Trailer page snapshot" };
            pageSnapshot.Create();
            pageMaterial = new Material(turnShader);
            page = NewRect("Shop page turn",film).gameObject.AddComponent<RawImage>();
            page.texture = pageSnapshot;
            page.material = pageMaterial;
            page.raycastTarget = false;
            page.gameObject.SetActive(false);

            studioDim = NewRect("Studio fade",film).gameObject.AddComponent<Image>();
            studioDim.color = new Color(.015f,.025f,.055f,0);
            studioDim.raycastTarget = false;

            var root = NewRect("Flight into Gaming1 title",film);
            ending = root.gameObject.AddComponent<CanvasGroup>();
            ending.alpha = 0;
            ending.blocksRaycasts = false;
            city = CloneNode(sourceBg,root,"Bg - original title city");
            cityOpacity = city.gameObject.AddComponent<CanvasGroup>();
            var air = NewRect("Four pixel cloud planes and speed lines",root);
            atmosphere = air.gameObject.AddComponent<CanvasGroup>();
            for(int i=0;i<clouds.Length;i++)
            {
                var cloud=NewRect("2D cloud plane "+(i+1),air);
                cloud.sizeDelta=new Vector2(480+i%2*180,160+i%2*55);
                var graphic=cloud.gameObject.AddComponent<TrailerCloudGraphic>();
                graphic.color=i%2==0 ? new Color(.79f,.97f,.96f,.93f) : new Color(.48f,.79f,.87f,.88f);
                graphic.raycastTarget=false;
                clouds[i]=cloud;
                cloudGraphics[i]=graphic;
            }
            for(int i=0;i<lines.Length;i++)
            {
                var line = NewRect("Vertical speed line "+i,air);
                line.sizeDelta = new Vector2(2+(i%3)*2,110+(i*71%330));
                var graphic = line.gameObject.AddComponent<Image>();
                graphic.color = new Color(.8f,1,1,.2f+(i%4)*.11f);
                graphic.raycastTarget = false;
                lines[i]=line;
            }
            start = CloneNode(sourceStart,root,"Start - original BeginPanel node");
            startOpacity = start.gameObject.AddComponent<CanvasGroup>();
            // The sign occupies this region of the original full-canvas Start sprite.
            // Keep the original child label, frame and decoration completely intact.
            glow = NewRect("Start breathing perimeter",start).gameObject.AddComponent<Image>();
            glow.rectTransform.anchoredPosition = new Vector2(28,25);
            glow.rectTransform.sizeDelta = new Vector2(880,440);
            glowMaterial = new Material(glowShader);
            glow.material = glowMaterial;
            glow.raycastTarget = false;
            glow.color = new Color(.49f,1,.91f,1);
            glow.enabled = false;
            root.gameObject.SetActive(false);
        }

        // Kept for compatibility with the preceding trailer revision.
        public void ConfigureOpeningAndFog(RectTransform film,Sprite logo,Shader cloudShader)
        {
            ConfigureOpeningAndClouds(film,logo,null);
        }

        public void ConfigureOpeningAndClouds(RectTransform film,Sprite logo,Shader motionShader)
        {
            if(motionShader!=null)
            {
                cityMotionMaterial=new Material(motionShader);
                city.GetComponent<Image>().material=cityMotionMaterial;
            }
            var root=NewRect("Opening - original studio Logo",film);
            logoRoot=root.gameObject.AddComponent<CanvasGroup>();
            logoRoot.blocksRaycasts=false;
            var black=root.gameObject.AddComponent<Image>();
            black.color=Color.black;
            black.raycastTarget=false;
            logoImage=NewRect("Resources Logo",root).gameObject.AddComponent<Image>();
            logoImage.sprite=logo;
            logoImage.raycastTarget=false;
            logoImage.color=new Color(1,1,1,0);
        }

        public void TickLogo(float t,float duration)
        {
            logoImage.color=new Color(1,1,1,Smooth(t/.6f));
            logoRoot.alpha=1-Smooth((t-duration+.65f)/.65f);
            logoImage.transform.localScale=Vector3.one*Mathf.Lerp(.97f,1.02f,Smooth(t/duration));
        }

        public void HideLogo() { if(logoRoot!=null) logoRoot.gameObject.SetActive(false); }

        public void TurnPage(RenderTexture source,Rect uv,float now,float duration=PageTurnDuration)
        {
            Graphics.Blit(source,pageSnapshot);
            page.uvRect = uv;
            pageStarted = now;
            pageDuration = duration;
            pageMaterial.SetFloat("_Progress",0);
            page.gameObject.SetActive(true);
        }

        public void TickPage(float now)
        {
            if(!page.gameObject.activeSelf) return;
            float p = Mathf.Clamp01((now-pageStarted)/pageDuration);
            pageMaterial.SetFloat("_Progress",p);
            if(p>=1) page.gameObject.SetActive(false);
        }

        public void DimStudio(float opacity)
        {
            studioDim.color = new Color(0,0,0,opacity);
        }

        public void BeginFlight()
        {
            flying = true;
            ending.gameObject.SetActive(true);
            ending.alpha = 1;
            TickFlight(0);
        }

        public void TickFlight(float t)
        {
            if(!flying) return;
            // A single distance curve drives camera, cloud planes and streaks: slow, fast, slow.
            float distance=FlightProgress(t);
            float velocity=Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Clamp01(t/4)),4);
            float cityIn = Smooth((t-1.55f)/.95f);
            cityOpacity.alpha = cityIn;
            city.anchoredPosition = new Vector2(0,240*(1-distance));
            city.localScale = Vector3.one*Mathf.Lerp(1.48f,1,distance);
            DimStudio(1-cityIn);
            float rise = Smoother((t-1.55f)/2.35f);
            startOpacity.alpha = Smooth((t-1.65f)/.8f);
            start.anchoredPosition = new Vector2(0,Mathf.Lerp(-610,0,rise));
            start.localScale = Vector3.one*Mathf.Lerp(.86f,1,rise);

            float airAlpha = Smooth(t/.3f)*(1-Smooth((t-2.65f)/1.1f));
            atmosphere.alpha = airAlpha;
            float travel = distance*7800;
            if(cityMotionMaterial!=null) cityMotionMaterial.SetFloat("_BlurPixels",velocity*60);
            for(int i=0;i<clouds.Length;i++)
            {
                // Sparse silhouettes at the edges leave the central title readable.
                float x=i%2==0 ? -730+i*65 : 740-i*60;
                float y=Mathf.Repeat(1350+i*560-travel*(.63f+i*.11f),2600)-1300;
                clouds[i].anchoredPosition=new Vector2(x,y);
                cloudGraphics[i].SetMotionBlur(velocity*(100+i*15));
            }
            for(int i=0;i<lines.Length;i++)
            {
                float x=-920+(i*137%1840);
                float y=Mathf.Repeat(i*197-travel*(1.8f+i%3*.25f),1700)-850;
                lines[i].anchoredPosition = new Vector2(x,y);
                lines[i].localScale = new Vector3(1,Mathf.Lerp(.15f,2.8f,velocity),1);
            }
            glow.enabled = t>=3.6f;
            if(glow.enabled) glowMaterial.SetFloat("_Reveal",Smooth((t-3.6f)/.6f));
        }

        public static float FlightProgress(float seconds) => Smoother(seconds/4);
        private static float Smoother(float v) { v=Mathf.Clamp01(v); return v*v*v*(v*(v*6-15)+10); }
        private static float Smooth(float v) => Mathf.SmoothStep(0,1,Mathf.Clamp01(v));

        private static RectTransform CloneNode(RectTransform source,Transform parent,string name)
        {
            var result=Instantiate(source,parent,false);
            result.name=name;
            result.anchorMin=result.anchorMax=new Vector2(.5f,.5f);
            result.pivot=new Vector2(.5f,.5f);
            result.sizeDelta=new Vector2(1920,1080);
            result.anchoredPosition=Vector2.zero;
            result.localScale=Vector3.one;
            result.localRotation=Quaternion.identity;
            foreach(var selectable in result.GetComponentsInChildren<Selectable>(true)) selectable.enabled=false;
            foreach(var graphic in result.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget=false;
            result.gameObject.SetActive(true);
            return result;
        }

        private static RectTransform NewRect(string name,Transform parent)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);
            r.sizeDelta=new Vector2(1920,1080);
            return r;
        }

        private void OnDestroy()
        {
            if(pageSnapshot!=null) { pageSnapshot.Release(); Destroy(pageSnapshot); }
            if(pageMaterial!=null) Destroy(pageMaterial);
            if(glowMaterial!=null) Destroy(glowMaterial);
            if(cityMotionMaterial!=null) Destroy(cityMotionMaterial);
        }
    }
}
