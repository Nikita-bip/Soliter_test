using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Models
{
    public sealed class PileModel
    {
        public int Index;
        public readonly List<CardModel> CardsTopToBottom = new();

        public int Capacity => CardsTopToBottom.Count;

        public CardModel GetTopExposedCard()
        {
            return CardsTopToBottom.FirstOrDefault(card => card.IsExposed);
        }
    }
}