using System;
using UnityEngine;
using UnityEngine.UI;

namespace Anchor.Trailer
{
    /// <summary>Shows the four authored ending artwork sets without writing a leaderboard result.</summary>
    public sealed class TrailerEndingGallery : MonoBehaviour
    {
        [Serializable]
        public sealed class Artwork
        {
            public string id;
            public Sprite background;
            public Sprite result;
            public Sprite character;
        }

        private RectTransform root, art;
        private Image background, result, character;
        private TrailerFireworksGraphic fireworks;
        private Artwork[] endings;
        private int current;

        public void Build(Canvas canvas,Artwork[] artwork)
        {
            endings=artwork;
            root=Rect("Four original game endings",canvas.transform);
            art=Rect("Ending artwork - slow zoom and elastic return",root);
            background=Picture("Original ending Bg",art);
            result=Picture("Original ending Result",art);
            character=Picture("Original ending Idle",art);
            // These transparent sprites were painted on a common 6400x3600 canvas.
            // Preserve their authored alignment and leave space for the film captions.
            result.rectTransform.localScale=Vector3.one*.91f;
            result.rectTransform.anchoredPosition=new Vector2(0,-48);
            character.rectTransform.localScale=Vector3.one*.91f;
            character.rectTransform.anchoredPosition=new Vector2(-24,38);
            fireworks=Rect("Best ending - pixel fireworks",root).gameObject.AddComponent<TrailerFireworksGraphic>();
            fireworks.raycastTarget=false;
            root.gameObject.SetActive(false);
        }

        public void Show(int page)
        {
            current=page;
            root.gameObject.SetActive(true);
            background.sprite=endings[page].background;
            result.sprite=endings[page].result;
            character.sprite=endings[page].character;
            Tick(0,2.4f);
            Debug.Log($"[Trailer] Ending {page+1}/4: {endings[page].id}");
        }

        public void Tick(float t,float duration)
        {
            float scale;
            if(current<3) scale=Mathf.Lerp(1,1.065f,Mathf.Clamp01(t/duration));
            else if(t<1.45f) scale=Mathf.Lerp(1,1.10f,Mathf.SmoothStep(0,1,t/1.45f));
            else
            {
                float settle=t-1.45f;
                scale=1+.10f*Mathf.Exp(-4.6f*settle)*Mathf.Cos(15.5f*settle);
                if(settle>1.6f) scale=1;
            }
            art.localScale=Vector3.one*scale;
            fireworks.SetTime(current==3 ? t-1.45f : -1);
        }

        public void Hide() { root.gameObject.SetActive(false); }

        private void LateUpdate()
        {
            if(root==null) return;
            var parent=(RectTransform)root.parent;
            root.localScale=new Vector3(parent.rect.width/1920,parent.rect.height/1080,1);
        }

        private static RectTransform Rect(string name,Transform parent)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);
            r.sizeDelta=new Vector2(1920,1080);
            return r;
        }

        private static Image Picture(string name,Transform parent)
        {
            var image=Rect(name,parent).gameObject.AddComponent<Image>();
            image.raycastTarget=false;
            return image;
        }
    }
}
