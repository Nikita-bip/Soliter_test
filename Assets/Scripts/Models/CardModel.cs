using Assets.Scripts.Views;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public sealed class CardModel
    {
        public int Id;
        public int PileIndex;
        public int IndexInPile;
        public CardDescriptor Descriptor;
        public CardModel Parent;
        public CardModel Child;
        public bool IsOpen;
        public bool IsRemoved;
        public Vector2 InitialAnchoredPosition;
        public CardView View;

        public bool IsExposed => !IsRemoved && (Parent == null || Parent.IsRemoved);

        public override string ToString()
        {
            return $"Card {Id} | pile {PileIndex} | idx {IndexInPile} | {Descriptor.Rank.ToShortString()}";
        }
    }
}