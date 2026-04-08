using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using TestTask.Solitaire.Config;
using TestTask.Solitaire.Models;
using TestTask.Solitaire.Views;
using UnityEngine;

namespace TestTask.Solitaire.Controllers
{
    public sealed class SolitaireController : MonoBehaviour
    {
        [Header("Scene refs")]
        [SerializeField] private CardView[] sceneCards;
        [SerializeField] private CardSpriteLibrary spriteLibrary;
        [SerializeField] private RectTransform currentCardAnchor;
        [SerializeField] private CurrentCardSlotView currentCardSlotView;
        [SerializeField] private BankView bankView;
        [SerializeField] private HUDView hudView;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private SolitaireInputRouter inputRouter;
        [SerializeField] private SolitaireAudio solitaireAudio;

        [Header("Settings")]
        [SerializeField] private SolitaireGeneratorSettings settings = new();
        [SerializeField] private bool randomizeSeedOnStart = true;
        [SerializeField] private int fixedSeed = 12345;

        private GameModel _model;
        private int _currentSeed;

        public IReadOnlyList<CardModel> ActiveCards => _model?.AllCards;
        public Camera UICamera => uiCamera;
        public bool IsBusy => _model != null && _model.IsBusy;

        private async void Start()
        {
            DOTween.SetTweensCapacity(200, 50);
            BuildStaticModel();
            await RestartLevelAsync();
        }

        [ContextMenu("Restart")]
        public async void RestartFromInspector()
        {
            await RestartLevelAsync();
        }

        public async Task RestartLevelAsync()
        {
            if (_model == null)
            {
                BuildStaticModel();
            }

            _currentSeed = randomizeSeedOnStart ? Environment.TickCount : fixedSeed;

            CombinationGenerator.Fill(_model, settings, _currentSeed);
            _model.ResetRuntimeState();

            hudView.ResetPanels();
            currentCardSlotView.SetSpriteLibrary(spriteLibrary);

            bankView.SetSpriteLibrary(spriteLibrary);
            bankView.HideAnimatedCard();

            currentCardSlotView.Show(_model.CurrentDescriptor);
            RefreshBankView();

            foreach (var card in _model.AllCards)
            {
                card.View.transform.DOKill();
                card.View.SetSpriteLibrary(spriteLibrary);
                card.View.SetDescriptor(card.Descriptor, false);
                card.View.ResetVisualState(faceUp: false, visible: true);
                card.View.SetInteractable(false);
            }

            // Изначально открываем только верхние карты.
            foreach (var pile in _model.Piles)
            {
                var top = pile.GetTopExposedCard();
                if (top == null)
                {
                    continue;
                }

                top.IsOpen = true;
                top.View.SetDescriptor(top.Descriptor, true);
                top.View.ResetVisualState(faceUp: true, visible: true);
            }

            inputRouter.Initialize(this);
            await Task.Yield();
        }

        public bool TryHandlePointer(Vector2 screenPoint)
        {
            if (_model == null || !_model.IsPlaying || _model.IsBusy)
            {
                return false;
            }

            if (_model.CanOpenNextBank)
            {
                if (bankView != null && bankView.ContainsScreenPoint(screenPoint, uiCamera))
                {
                    _ = TryUseBankAsync();
                    return true;
                }

                return false;
            }

            var exposedCards = _model.AllCards
                .Where(card => card.IsOpen && card.IsExposed && !card.IsRemoved)
                .OrderByDescending(card => card.View.RectTransform.GetSiblingIndex())
                .ToList();

            foreach (var card in exposedCards)
            {
                if (!card.View.ContainsScreenPoint(screenPoint, uiCamera))
                {
                    continue;
                }

                _ = TryTakeCardAsync(card);
                return true;
            }

            if (bankView != null && bankView.ContainsScreenPoint(screenPoint, uiCamera))
            {
                _ = TryUseBankAsync();
                return true;
            }

            return false;
        }

        private void BuildStaticModel()
        {
            if (sceneCards == null || sceneCards.Length == 0)
            {
                sceneCards = GetComponentsInChildren<CardView>(true);
            }

            _model = LayoutAnalyzer.Analyze(sceneCards, settings.pileGroupingThreshold);

            foreach (var card in _model.AllCards)
            {
                card.View.SetSpriteLibrary(spriteLibrary);
            }
        }

        private async Task TryTakeCardAsync(CardModel card)
        {
            if (_model.IsBusy || !card.IsOpen || !card.IsExposed || card.IsRemoved)
            {
                return;
            }

            if (_model.CanOpenNextBank)
            {
                return;
            }

            if (card.ComboIndex != _model.CurrentComboIndex)
            {
                return;
            }

            if (!_model.CurrentDescriptor.Rank.IsAdjacentCyclic(card.Descriptor.Rank))
            {
                return;
            }

            _model.IsBusy = true; 
            solitaireAudio?.PlayTakeCard();
            card.View.transform.SetAsLastSibling();

            await card.View.FlyToAsync(currentCardAnchor, settings.moveDuration);
            await card.View.FadeOutAsync(settings.flipDuration);

            card.IsRemoved = true;
            _model.CurrentDescriptor = card.Descriptor;
            currentCardSlotView.Show(_model.CurrentDescriptor);
            _model.TakenInCurrentCombo++;

            if (card.Child != null && !card.Child.IsRemoved)
            {
                card.Child.IsOpen = true;
                card.Child.View.SetDescriptor(card.Child.Descriptor, false);
                await card.Child.View.FlipAsync(true, settings.flipDuration);
                await Task.Delay(TimeSpan.FromSeconds(settings.revealDelay));
            }

            RefreshBankView();

            if (_model.RemainingCards == 0)
            {
                _model.IsPlaying = false;
                hudView.ShowWin();
                _model.IsBusy = false;
                return;
            }

            if (!HasAvailableMove() && !_model.CanOpenNextBank)
            {
                _model.IsPlaying = false;
                hudView.ShowLose();
                _model.IsBusy = false;
                return;
            }

            _model.IsBusy = false;
        }

        private async Task TryUseBankAsync()
        {
            if (_model.IsBusy || !_model.CanOpenNextBank)
            {
                return;
            }

            _model.IsBusy = true;
            solitaireAudio?.PlayBank();

            var nextDescriptor = _model.BankSequence[_model.NextBankIndex];

            await bankView.PlayDrawToAsync(
                nextDescriptor,
                currentCardAnchor,
                settings.moveDuration,
                settings.flipDuration);

            _model.CurrentDescriptor = nextDescriptor;
            _model.NextBankIndex++;
            _model.CurrentComboIndex++;
            _model.TakenInCurrentCombo = 0;

            currentCardSlotView.Show(_model.CurrentDescriptor);

            RefreshBankView();

            await Task.Delay(System.TimeSpan.FromSeconds(settings.revealDelay));

            if (!HasAvailableMove() && !_model.CanOpenNextBank)
            {
                _model.IsPlaying = false;
                hudView.ShowLose();
            }

            _model.IsBusy = false;
        }

        private bool HasAvailableMove()
        {
            return _model.AllCards.Any(card =>
                card.IsOpen &&
                card.IsExposed &&
                !card.IsRemoved &&
                card.ComboIndex == _model.CurrentComboIndex &&
                _model.CurrentDescriptor.Rank.IsAdjacentCyclic(card.Descriptor.Rank));
        }

        private void RefreshBankView()
        {
            bankView.SetRemaining(_model.RemainingBankCards);
            bankView.SetAvailable(_model.CanOpenNextBank);
        }

        public void OnRestartButtonPressed()
        {
            if (_model != null && _model.IsBusy)
            {
                return;
            }

            solitaireAudio?.StopAll();
            RestartFromInspector();
        }
    }
}
