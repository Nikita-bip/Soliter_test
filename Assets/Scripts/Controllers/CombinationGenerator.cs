using Assets.Scripts.Config;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Controllers
{
    public static class CombinationGenerator
    {
        private static List<int> BuildComboLengths(int boardCardsCount, SolitaireGeneratorSettings settings, Random random)
        {
            var result = new List<int>();
            var remaining = boardCardsCount;
            var boardMin = settings.minCombinationLength - 1;
            var boardMax = settings.maxCombinationLength - 1;

            while (remaining > 0)
            {
                var possibleBoardCounts = new List<int>();

                for (var boardPart = boardMin; boardPart <= boardMax; boardPart++)
                {
                    if (boardPart > remaining)
                    {
                        break;
                    }

                    var nextRemaining = remaining - boardPart;
                    if (nextRemaining == 0 || nextRemaining >= boardMin)
                    {
                        possibleBoardCounts.Add(boardPart);
                    }
                }

                var chosenBoardCount = possibleBoardCounts[random.Next(possibleBoardCounts.Count)];
                result.Add(chosenBoardCount + 1);
                remaining -= chosenBoardCount;
            }

            return result;
        }

        private static void DistributeSolutionAcrossPiles(GameModel model, IReadOnlyList<CardDescriptor> boardSolution, Random random)
        {
            var perPile = model.Piles
                .Select(pile => new List<CardDescriptor>(pile.Capacity))
                .ToList();

            var remainingCapacity = model.Piles
                .Select(pile => pile.Capacity)
                .ToArray();

            var availablePileIndices = model.Piles
                .Select((_, index) => index)
                .Where(index => remainingCapacity[index] > 0)
                .OrderBy(_ => random.Next())
                .ToList();

            var cursor = 0;

            foreach (var pileIndex in availablePileIndices)
            {
                if (cursor >= boardSolution.Count)
                {
                    break;
                }

                perPile[pileIndex].Add(boardSolution[cursor++]);
                remainingCapacity[pileIndex]--;
            }

            while (cursor < boardSolution.Count)
            {
                var candidates = new List<int>();

                for (var i = 0; i < remainingCapacity.Length; i++)
                {
                    if (remainingCapacity[i] > 0)
                    {
                        candidates.Add(i);
                    }
                }

                var chosenPile = candidates[random.Next(candidates.Count)];
                perPile[chosenPile].Add(boardSolution[cursor++]);
                remainingCapacity[chosenPile]--;
            }

            for (var pileIndex = 0; pileIndex < model.Piles.Count; pileIndex++)
            {
                var pile = model.Piles[pileIndex];
                var assigned = perPile[pileIndex];

                for (var cardIndex = 0; cardIndex < pile.CardsTopToBottom.Count; cardIndex++)
                {
                    pile.CardsTopToBottom[cardIndex].Descriptor = assigned[cardIndex];
                }
            }
        }

        private static CardDescriptor CreateRandomDescriptor(Random random)
        {
            return new CardDescriptor((CardRank)random.Next(1, 14), CreateRandomSuit(random));
        }

        private static CardSuit CreateRandomSuit(Random random)
        {
            return (CardSuit)random.Next(0, 4);
        }

        public static void Fill(GameModel model, SolitaireGeneratorSettings settings, int seed)
        {
            var random = new Random(seed);
            model.BankSequence.Clear();

            var comboLengths = BuildComboLengths(model.AllCards.Count, settings, random);
            var boardSolution = new List<CardDescriptor>(model.AllCards.Count);

            foreach (var comboLength in comboLengths)
            {
                var start = CreateRandomDescriptor(random);
                model.BankSequence.Add(start);

                var boardCardsInCombo = comboLength - 1;
                var direction = random.NextDouble() <= settings.upwardChance ? 1 : -1;
                var currentRank = start.Rank;

                for (var i = 0; i < boardCardsInCombo; i++)
                {
                    if (i > 0 && random.NextDouble() <= settings.directionFlipChance)
                    {
                        direction *= -1;
                    }

                    currentRank = currentRank.Shift(direction);
                    boardSolution.Add(new CardDescriptor(currentRank, CreateRandomSuit(random)));
                }
            }

            DistributeSolutionAcrossPiles(model, boardSolution, random);
        }
    }
}