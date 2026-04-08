using System.Collections.Generic;
using System.Linq;

namespace TestTask.Solitaire.Models
{
    public sealed class GameModel
    {
        public readonly List<PileModel> Piles = new();
        public readonly List<CardModel> AllCards = new();
        public readonly List<CardDescriptor> BankSequence = new();
        public readonly List<int> BoardCardsPerCombo = new();

        public int NextBankIndex;
        public int CurrentComboIndex;
        public int TakenInCurrentCombo;
        public CardDescriptor CurrentDescriptor;
        public bool IsPlaying;
        public bool IsBusy;

        public int RemainingCards => AllCards.Count(card => !card.IsRemoved);
        public int RemainingBankCards => BankSequence.Count - NextBankIndex;

        public bool CanOpenNextBank =>
            IsPlaying &&
            CurrentComboIndex < BoardCardsPerCombo.Count &&
            TakenInCurrentCombo >= BoardCardsPerCombo[CurrentComboIndex] &&
            NextBankIndex < BankSequence.Count;

        public void ResetRuntimeState()
        {
            foreach (var card in AllCards)
            {
                card.IsRemoved = false;
                card.IsOpen = false;
            }

            NextBankIndex = 1;
            CurrentComboIndex = 0;
            TakenInCurrentCombo = 0;
            CurrentDescriptor = BankSequence[0];
            IsPlaying = true;
            IsBusy = false;
        }
    }
}
