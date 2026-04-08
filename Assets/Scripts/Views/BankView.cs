using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Models;

namespace Assets.Scripts.Views
{
    public sealed class BankView : MonoBehaviour
    {
        [Header("Logic")]
        [SerializeField] private RectTransform hitBox;
        [SerializeField] private TMP_Text counterText;

        [Header("Pile Visuals")]
        [SerializeField] private RectTransform drawFromAnchor;
        [SerializeField] private List<GameObject> stackCards = new();

        [Header("Animated Card")]
        [SerializeField] private RectTransform animatedCardRoot;
        [SerializeField] private Image animatedCardImage;
        [SerializeField] private CanvasGroup animatedCardCanvasGroup;

        private CardSpriteLibrary _spriteLibrary;

        public RectTransform HitBox => hitBox;

        public void SetSpriteLibrary(CardSpriteLibrary spriteLibrary)
        {
            _spriteLibrary = spriteLibrary;
        }

        public void SetRemaining(int count)
        {
            if (counterText != null)
            {
                counterText.text = count.ToString();
            }

            UpdateStackVisual(count);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera fallbackCamera)
        {
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

            return RectTransformUtility.RectangleContainsScreenPoint(hitBox, screenPoint, eventCamera);
        }

        public void HideAnimatedCard()
        {
            if (animatedCardRoot != null)
            {
                animatedCardRoot.gameObject.SetActive(false);
            }
        }

        public async Task PlayDrawToAsync(
            CardDescriptor descriptor,
            RectTransform target,
            float moveDuration,
            float flipDuration)
        {
            if (animatedCardRoot == null ||
                animatedCardImage == null ||
                animatedCardCanvasGroup == null ||
                target == null ||
                _spriteLibrary == null)
            {
                return;
            }

            animatedCardRoot.gameObject.SetActive(true);
            animatedCardRoot.SetAsLastSibling();

            animatedCardRoot.position = drawFromAnchor != null
                ? drawFromAnchor.position
                : hitBox.position;

            animatedCardRoot.localScale = Vector3.one;
            animatedCardCanvasGroup.alpha = 1f;
            animatedCardImage.sprite = _spriteLibrary.CardBack;

            await WaitTween(
                animatedCardRoot.DOMove(target.position, moveDuration)
                    .SetEase(Ease.OutCubic));

            var halfFlip = flipDuration * 0.5f;

            await WaitTween(
                animatedCardRoot.DOScaleX(0f, halfFlip)
                    .SetEase(Ease.InQuad));

            animatedCardImage.sprite = _spriteLibrary.GetFace(descriptor);

            await WaitTween(
                animatedCardRoot.DOScaleX(1f, halfFlip)
                    .SetEase(Ease.OutQuad));

            animatedCardRoot.gameObject.SetActive(false);
        }

        private void UpdateStackVisual(int remainingCount)
        {
            if (stackCards == null || stackCards.Count == 0)
            {
                return;
            }

            var visibleCount = Mathf.Clamp(remainingCount, 0, stackCards.Count);

            for (var i = 0; i < stackCards.Count; i++)
            {
                if (stackCards[i] != null)
                {
                    stackCards[i].SetActive(i < visibleCount);
                }
            }
        }

        public void ShowHintPulse()
        {
            ClearHint();

            transform.DOKill();
            transform.localScale = Vector3.one;

            transform
                .DOScale(1.05f, 0.3f)
                .SetLoops(4, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        public void ClearHint()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;

            if (animatedCardRoot != null)
            {
                animatedCardRoot.DOKill();
            }
        }
        private static Task WaitTween(Tween tween)
        {
            var tcs = new TaskCompletionSource<bool>();

            tween.onComplete += () => tcs.TrySetResult(true);
            tween.onKill += () => tcs.TrySetResult(true);

            return tcs.Task;
        }

        private void OnDisable()
        {
            ClearHint();
        }
    }
}