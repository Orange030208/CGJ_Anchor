using UnityEngine;
using UnityEngine.UI;

namespace Anchor.Trailer
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TrailerFireworksGraphic : MaskableGraphic
    {
        private float elapsed=-1;
        private static readonly Vector2[] Centers =
        {
            new Vector2(-580,100),new Vector2(180,230),new Vector2(-100,15),
            new Vector2(600,320),new Vector2(-660,300),new Vector2(400,-140),new Vector2(-260,260)
        };
        private static readonly Color[] Palette =
        {
            new Color(1,.75f,.22f),new Color(.4f,1,.84f),new Color(1,.38f,.7f),new Color(.45f,.7f,1)
        };

        public void SetTime(float time) { elapsed=time; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(elapsed<0) return;
            for(int burst=0;burst<Centers.Length;burst++)
            {
                float t=elapsed-burst*.34f;
                if(t<0 || t>1.45f) continue;
                Color tint=Palette[burst%Palette.Length];
                // A bright pixel cross announces each burst before its square particles spread.
                if(t<.16f)
                {
                    Color flash=Color.Lerp(tint,Color.white,.7f); flash.a=1-t/.16f;
                    Quad(vh,Centers[burst],new Vector2(65,7),flash);
                    Quad(vh,Centers[burst],new Vector2(7,65),flash);
                }
                for(int ray=0;ray<32;ray++)
                    for(int trail=2;trail>=0;trail--)
                    {
                        float age=Mathf.Max(0,t-trail*.045f);
                        float angle=ray*Mathf.PI*2/32+burst*.37f;
                        float speed=175+(ray*47%95);
                        float radius=speed*(1-Mathf.Exp(-age*2.4f));
                        Vector2 position=Centers[burst]+new Vector2(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius-age*age*85);
                        position=new Vector2(Mathf.Round(position.x/5)*5,Mathf.Round(position.y/5)*5);
                        Color c=Color.Lerp(tint,Color.white,trail==0?.35f:0);
                        c.a=Mathf.Clamp01((1.45f-t)/.65f)*(trail==0?1:.28f)*(ray%3==0?.75f:1);
                        float size=trail==0?7+(ray%3)*2:5;
                        Quad(vh,position,new Vector2(size,size),c);
                    }
            }
        }

        private static void Quad(VertexHelper vh,Vector2 p,Vector2 size,Color c)
        {
            int v=vh.currentVertCount;
            Vector2 a=p-size*.5f,b=p+size*.5f;
            vh.AddVert(new Vector3(a.x,a.y),c,Vector2.zero);
            vh.AddVert(new Vector3(a.x,b.y),c,Vector2.zero);
            vh.AddVert(new Vector3(b.x,b.y),c,Vector2.zero);
            vh.AddVert(new Vector3(b.x,a.y),c,Vector2.zero);
            vh.AddTriangle(v,v+1,v+2); vh.AddTriangle(v,v+2,v+3);
        }
    }
}
