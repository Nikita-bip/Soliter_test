using System.Threading.Tasks;
using Assets.Scripts.Models;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Views
{
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private Image faceImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image highlightImage;

        private RectTransform _rectTransform;
        private CardSpriteLibrary _spriteLibrary;
        private CardDescriptor _descriptor;
        private Vector2 _initialAnchoredPosition;
        private int _initialSiblingIndex;

        public RectTransform RectTransform => _rectTransform != null
            ? _rectTransform
            : _rectTransform = GetComponent<RectTransform>();

        public Vector2 InitialAnchoredPosition => _initialAnchoredPosition;
        public bool IsFaceUp { get; private set; }
        public bool IsInteractable { get; private set; }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialAnchoredPosition = _rectTransform.anchoredPosition;
            _initialSiblingIndex = transform.GetSiblingIndex();
        }

        public void CacheInitialPosition()
        {
            _initialAnchoredPosition = RectTransform.anchoredPosition;
            _initialSiblingIndex = transform.GetSiblingIndex();
        }

        public void SetSpriteLibrary(CardSpriteLibrary spriteLibrary)
        {
            _spriteLibrary = spriteLibrary;
        }

        public void SetDescriptor(CardDescriptor descriptor, bool faceUp)
        {
            _descriptor = descriptor;
            IsFaceUp = faceUp;

            NormalizeScale();
            RefreshSprite();
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }

            gameObject.SetActive(visible);
        }

        public void SetInteractable(bool interactable)
        {
            IsInteractable = interactable;
        }

        public void ResetVisualState(bool faceUp, bool visible)
        {
            RectTransform.DOKill();

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
            }

            ClearHint();

            transform.SetSiblingIndex(_initialSiblingIndex);

            RectTransform.anchoredPosition = _initialAnchoredPosition;
            RectTransform.localRotation = Quaternion.identity;
            RectTransform.localScale = Vector3.one;

            if (faceImage != null)
            {
                faceImage.rectTransform.localRotation = Quaternion.identity;
                faceImage.rectTransform.localScale = Vector3.one;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }

            gameObject.SetActive(visible);

            IsFaceUp = faceUp;
            RefreshSprite();
            SetInteractable(false);
            ClearHint();
        }


        public void ClearHint()
        {
            if (highlightImage == null)
            {
                return;
            }

            highlightImage.DOKill();
            highlightImage.rectTransform.DOKill();

            var color = highlightImage.color;
            color.a = 0.5f;
            highlightImage.color = color;

            highlightImage.rectTransform.localScale = Vector3.one;
            highlightImage.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            ClearHint();
        }

        public void ShowHintPulse()
        {
            if (highlightImage == null)
            {
                return;
            }

            ClearHint();

            var color = highlightImage.color;
            color.a = 0.5f;
            highlightImage.color = color;

            highlightImage.gameObject.SetActive(true);
            highlightImage.rectTransform.localScale = Vector3.one;

            highlightImage.rectTransform
                .DOScale(1.08f, 0.35f)
                .SetLoops(4, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        public void SetHintVisible(bool visible)
        {
            if (highlightImage != null)
            {
                highlightImage.gameObject.SetActive(visible);
            }
        }

        public async Task FlipAsync(bool faceUp, float duration)
        {
            RectTransform.DOKill();
            NormalizeScale();

            if (IsFaceUp == faceUp)
            {
                RefreshSprite();
                return;
            }

            var half = duration * 0.5f;

            await WaitTween(
                RectTransform.DOScaleX(0f, half)
                    .SetEase(Ease.InQuad));

            RectTransform.localScale = new Vector3(0f, 1f, 1f);

            IsFaceUp = faceUp;
            RefreshSprite();

            await WaitTween(
                RectTransform.DOScaleX(1f, half)
                    .SetEase(Ease.OutQuad));

            NormalizeScale();
        }

        public async Task MoveToWorldAsync(Vector3 worldPosition, float duration)
        {
            RectTransform.DOKill();

            await WaitTween(
                RectTransform.DOMove(worldPosition, duration)
                    .SetEase(Ease.OutCubic));

            NormalizeScale();
        }

        public async Task FlyToAsync(RectTransform target, float duration)
        {
            if (target == null)
            {
                return;
            }

            await MoveToWorldAsync(target.position, duration);
        }

        public async Task FadeOutAsync(float duration)
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

            canvasGroup.DOKill();

            await WaitTween(
                canvasGroup.DOFade(0f, duration)
                    .SetEase(Ease.Linear));

            gameObject.SetActive(false);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera fallbackCamera)
        {
            if (!gameObject.activeInHierarchy)
            {
                return false;
            }

            var canvas = GetComponentInParent<Canvas>();
            Camera eventCamera = fallbackCamera;

            if (canvas != null)
            {
                var rootCanvas = canvas.rootCanvas;
                if (rootCanvas != null)
                {
                    eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                        ? null
                        : rootCanvas.worldCamera;
                }
            }

            return RectTransformUtility.RectangleContainsScreenPoint(RectTransform, screenPoint, eventCamera);
        }

        private void RefreshSprite()
        {
            if (_spriteLibrary == null || faceImage == null)
            {
                return;
            }

            faceImage.sprite = IsFaceUp
                ? _spriteLibrary.GetFace(_descriptor)
                : _spriteLibrary.CardBack;
        }

        private void NormalizeScale()
        {
            var scale = RectTransform.localScale;
            scale.x = Mathf.Abs(scale.x);
            scale.y = Mathf.Abs(scale.y) <= Mathf.Epsilon ? 1f : Mathf.Abs(scale.y);
            scale.z = Mathf.Abs(scale.z) <= Mathf.Epsilon ? 1f : Mathf.Abs(scale.z);
            RectTransform.localScale = scale;

            if (faceImage != null)
            {
                var imageScale = faceImage.rectTransform.localScale;
                imageScale.x = Mathf.Abs(imageScale.x);
                imageScale.y = Mathf.Abs(imageScale.y) <= Mathf.Epsilon ? 1f : Mathf.Abs(imageScale.y);
                imageScale.z = Mathf.Abs(imageScale.z) <= Mathf.Epsilon ? 1f : Mathf.Abs(imageScale.z);
                faceImage.rectTransform.localScale = imageScale;
            }
        }

        private static Task WaitTween(Tween tween)
        {
            var tcs = new TaskCompletionSource<bool>();

            tween.onComplete += () => tcs.TrySetResult(true);
            tween.onKill += () => tcs.TrySetResult(true);

            return tcs.Task;
        }
    }
}