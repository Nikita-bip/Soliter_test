using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.Config;
using Assets.Scripts.Models;
using Assets.Scripts.Views;
using DG.Tweening;
using UnityEngine;

namespace Assets.Scripts.Controllers
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

        public void OnRestartButtonPressed()
        {
            if (_model != null && _model.IsBusy)
            {
                return;
            }

            RestartFromInspector();
        }

        public async Task RestartLevelAsync()
        {
            if (_model == null)
            {
                BuildStaticModel();
            }

            ClearAllHints();

            _currentSeed = randomizeSeedOnStart ? Environment.TickCount : fixedSeed;

            CombinationGenerator.Fill(_model, settings, _currentSeed);
            _model.ResetRuntimeState();

            hudView.ResetPanels();

            currentCardSlotView.SetSpriteLibrary(spriteLibrary);
            currentCardSlotView.Show(_model.CurrentDescriptor);

            if (bankView != null)
            {
                bankView.SetSpriteLibrary(spriteLibrary);
                bankView.HideAnimatedCard();
                bankView.ClearHint();
            }

            foreach (var card in _model.AllCards)
            {
                card.View.SetSpriteLibrary(spriteLibrary);
                card.View.SetDescriptor(card.Descriptor, false);
                card.View.ResetVisualState(faceUp: false, visible: true);
                card.View.SetInteractable(false);
            }

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

            RefreshBankView();
            RefreshHud();

            inputRouter.Initialize(this);
            await Task.Yield();
        }

        public bool TryHandlePointer(Vector2 screenPoint)
        {
            if (_model == null || !_model.IsPlaying || _model.IsBusy)
            {
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

            if (!_model.CurrentDescriptor.Rank.IsAdjacentCyclic(card.Descriptor.Rank))
            {
                return;
            }

            _model.IsBusy = true;
            solitaireAudio?.PlayTakeCard();
            card.View.transform.SetAsLastSibling();

            card.View.transform.SetAsLastSibling();

            await card.View.FlyToAsync(currentCardAnchor, settings.moveDuration);
            await card.View.FadeOutAsync(settings.flipDuration);

            card.IsRemoved = true;
            _model.CurrentDescriptor = card.Descriptor;
            currentCardSlotView.Show(_model.CurrentDescriptor);

            if (card.Child != null && !card.Child.IsRemoved)
            {
                card.Child.IsOpen = true;
                card.Child.View.SetDescriptor(card.Child.Descriptor, false);
                await card.Child.View.FlipAsync(true, settings.flipDuration);
                await Task.Delay(TimeSpan.FromSeconds(settings.revealDelay));
            }

            RefreshBankView();
            RefreshHud();

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

            if (bankView != null && currentCardAnchor != null)
            {
                await bankView.PlayDrawToAsync(
                    nextDescriptor,
                    currentCardAnchor,
                    settings.moveDuration,
                    settings.flipDuration);
            }

            _model.CurrentDescriptor = nextDescriptor;
            _model.NextBankIndex++;

            currentCardSlotView.Show(_model.CurrentDescriptor);

            RefreshBankView();
            RefreshHud();

            await Task.Delay(TimeSpan.FromSeconds(settings.revealDelay));

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
                _model.CurrentDescriptor.Rank.IsAdjacentCyclic(card.Descriptor.Rank));
        }

        public void ShowHint()
        {
            if (_model == null || !_model.IsPlaying || _model.IsBusy)
            {
                return;
            }

            ClearAllHints();

            var hint = HintSolver.GetBestMove(_model);

            switch (hint.Type)
            {
                case HintSolver.HintMoveType.TakeCard:
                    if (hint.Card != null)
                    {
                        hint.Card.View.ShowHintPulse();
                    }
                    break;

                case HintSolver.HintMoveType.OpenBank:
                    if (bankView != null)
                    {
                        bankView.ShowHintPulse();
                    }
                    break;

                case HintSolver.HintMoveType.None:
                    break;
            }
        }

        private void ClearAllHints()
        {
            if (_model == null)
            {
                return;
            }

            foreach (var card in _model.AllCards)
            {
                if (card?.View != null)
                {
                    card.View.ClearHint();
                }
            }
        }

        private void RefreshBankView()
        {
            if (bankView == null)
            {
                return;
            }

            bankView.SetRemaining(_model.RemainingBankCards);
        }

        private void RefreshHud()
        {
            if (hudView == null)
            {
                return;
            }
        }
    }
}