using TestTask.Solitaire.Views;
using UnityEngine;

namespace TestTask.Solitaire.Models
{
    public sealed class CardModel
    {
        public int Id;
        public int PileIndex;
        public int IndexInPile;
        public int ComboIndex;
        public CardDescriptor Descriptor;
        public CardModel Parent; // Карта, перекрывающая текущую.
        public CardModel Child;  // Карта, лежащая под текущей.
        public bool IsOpen;
        public bool IsRemoved;
        public Vector2 InitialAnchoredPosition;
        public CardView View;

        public bool IsExposed => !IsRemoved && (Parent == null || Parent.IsRemoved);

        public override string ToString()
        {
            return $"Card {Id} | pile {PileIndex} | idx {IndexInPile} | combo {ComboIndex} | {Descriptor.Rank.ToShortString()}";
        }
    }
}