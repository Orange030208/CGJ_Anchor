using UnityEngine;
using UnityEngine.UI;

namespace Anchor.Trailer
{
    /// <summary>Stepped 2D cloud silhouettes built as UI geometry, matching the pixel artwork.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TrailerCloudGraphic : MaskableGraphic
    {
        private float blurPixels;

        public void SetMotionBlur(float pixels)
        {
            pixels=Mathf.Max(0,pixels);
            if(Mathf.Abs(pixels-blurPixels)<.2f) return;
            blurPixels=pixels;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int samples=blurPixels<1 ? 1 : 9;
            for(int sample=0;sample<samples;sample++)
            {
                float offset=samples==1 ? 0 : (sample/8f-.5f)*blurPixels;
                float weight=samples==1 ? 1 : (sample==4?.2f:(sample==3||sample==5?.16f:(sample==2||sample==6?.12f:(sample==1||sample==7?.08f:.04f))));
                DrawSilhouette(vh,offset,weight);
            }
        }

        private void DrawSilhouette(VertexHelper vh,float offset,float weight)
        {
            var r=rectTransform.rect;
            const float step=9;
            // Union of four lobes, rasterized into horizontal strips: no overlapping alpha seams.
            for(float y=-r.height*.5f;y<r.height*.5f;y+=step)
            {
                float left=float.MaxValue,right=float.MinValue;
                for(int lobe=0;lobe<4;lobe++)
                {
                    float cx=r.width*(-.31f+lobe*.21f);
                    float cy=lobe==1?r.height*.05f:-r.height*.1f;
                    float rx=r.width*(lobe==1?.3f:.25f);
                    float ry=r.height*(lobe==1?.48f:.35f);
                    float dy=(y+step*.5f-cy)/ry;
                    if(Mathf.Abs(dy)>1) continue;
                    float span=rx*Mathf.Sqrt(1-dy*dy);
                    left=Mathf.Min(left,Mathf.Floor((cx-span)/step)*step);
                    right=Mathf.Max(right,Mathf.Ceil((cx+span)/step)*step);
                }
                if(right<=left) continue;
                Color tint=Color.Lerp(new Color(color.r*.58f,color.g*.73f,color.b*.82f,color.a),color,Mathf.Clamp01((y/r.height+.5f)*2));
                tint.a*=weight;
                int v=vh.currentVertCount;
                vh.AddVert(new Vector3(left,y+offset),tint,Vector2.zero);
                vh.AddVert(new Vector3(left,y+step+offset),tint,Vector2.zero);
                vh.AddVert(new Vector3(right,y+step+offset),tint,Vector2.zero);
                vh.AddVert(new Vector3(right,y+offset),tint,Vector2.zero);
                vh.AddTriangle(v,v+1,v+2); vh.AddTriangle(v,v+2,v+3);
            }
        }
    }
}
