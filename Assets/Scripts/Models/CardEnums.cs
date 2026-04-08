namespace TestTask.Solitaire.Models
{
    public enum CardRank
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
    }

    public enum CardSuit
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3,
    }

    public readonly struct CardDescriptor
    {
        public readonly CardRank Rank;
        public readonly CardSuit Suit;

        public CardDescriptor(CardRank rank, CardSuit suit)
        {
            Rank = rank;
            Suit = suit;
        }
    }

    public static class CardRankExtensions
    {
        public static CardRank Shift(this CardRank rank, int delta)
        {
            var value = (int)rank - 1;
            value = (value + delta) % 13;
            if (value < 0)
            {
                value += 13;
            }

            return (CardRank)(value + 1);
        }

        public static bool IsAdjacentCyclic(this CardRank source, CardRank target)
        {
            return source.Shift(1) == target || source.Shift(-1) == target;
        }

        public static string ToShortString(this CardRank rank)
        {
            return rank switch
            {
                CardRank.Ace => "A",
                CardRank.Jack => "J",
                CardRank.Queen => "Q",
                CardRank.King => "K",
                CardRank.Ten => "10",
                _ => ((int)rank).ToString(),
            };
        }
    }
}
