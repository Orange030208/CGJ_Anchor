using UnityEngine;
using DG.Tweening;

namespace Anchor.Trailer
{
    /// <summary>
    /// 宣传片摄像机控制器 - 处理镜头位置、缩放和平滑过渡
    /// </summary>
    public class TrailerCameraController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float defaultTransitionDuration = 1f;
        [SerializeField] private Ease defaultEase = Ease.InOutQuad;

        private Tweener positionTween;
        private Tweener sizeTween;

        public void SetCamera(Camera cam)
        {
            targetCamera = cam;
        }

        /// <summary>
        /// 立即设置摄像机位置（无动画）
        /// </summary>
        public void SetPositionImmediate(Vector3 position)
        {
            if (targetCamera != null)
            {
                targetCamera.transform.position = position;
            }
        }

        /// <summary>
        /// 立即设置正交相机尺寸（无动画）
        /// </summary>
        public void SetOrthoSizeImmediate(float size)
        {
            if (targetCamera != null && targetCamera.orthographic)
            {
                targetCamera.orthographicSize = size;
            }
        }

        /// <summary>
        /// 平滑移动摄像机到目标位置
        /// </summary>
        public void MoveTo(Vector3 targetPosition, float duration = -1f, Ease ease = Ease.Unset)
        {
            if (targetCamera == null) return;

            positionTween?.Kill();
            float d = duration > 0 ? duration : defaultTransitionDuration;
            Ease e = ease != Ease.Unset ? ease : defaultEase;
            positionTween = targetCamera.transform.DOMove(targetPosition, d).SetEase(e);
        }

        /// <summary>
        /// 平滑缩放正交相机
        /// </summary>
        public void ZoomTo(float targetSize, float duration = -1f, Ease ease = Ease.Unset)
        {
            if (targetCamera == null || !targetCamera.orthographic) return;

            sizeTween?.Kill();
            float d = duration > 0 ? duration : defaultTransitionDuration;
            Ease e = ease != Ease.Unset ? ease : defaultEase;
            sizeTween = DOTween.To(() => targetCamera.orthographicSize, x => targetCamera.orthographicSize = x, targetSize, d).SetEase(e);
        }

        /// <summary>
        /// 组合移动和缩放
        /// </summary>
        public void MoveAndZoom(Vector3 targetPosition, float targetSize, float duration = -1f, Ease ease = Ease.Unset)
        {
            MoveTo(targetPosition, duration, ease);
            ZoomTo(targetSize, duration, ease);
        }

        void OnDestroy()
        {
            positionTween?.Kill();
            sizeTween?.Kill();
        }
    }
}
