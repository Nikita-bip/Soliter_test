using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Controllers
{
    public static class HintSolver
    {
        public enum HintMoveType
        {
            None,
            TakeCard,
            OpenBank
        }

        public readonly struct HintMove
        {
            public readonly HintMoveType Type;
            public readonly CardModel Card;

            public HintMove(HintMoveType type, CardModel card = null)
            {
                Type = type;
                Card = card;
            }
        }

        private readonly struct SolverState : IEquatable<SolverState>
        {
            public readonly ulong RemovedMask;
            public readonly int CurrentRankValue;
            public readonly int NextBankIndex;

            public SolverState(ulong removedMask, int currentRankValue, int nextBankIndex)
            {
                RemovedMask = removedMask;
                CurrentRankValue = currentRankValue;
                NextBankIndex = nextBankIndex;
            }

            public bool Equals(SolverState other)
            {
                return RemovedMask == other.RemovedMask &&
                       CurrentRankValue == other.CurrentRankValue &&
                       NextBankIndex == other.NextBankIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is SolverState other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = RemovedMask.GetHashCode();
                    hash = (hash * 397) ^ CurrentRankValue;
                    hash = (hash * 397) ^ NextBankIndex;
                    return hash;
                }
            }
        }

        private static SolverState BuildCurrentState(GameModel model)
        {
            ulong removedMask = 0UL;

            for (var i = 0; i < model.AllCards.Count; i++)
            {
                if (model.AllCards[i].IsRemoved)
                {
                    removedMask |= 1UL << i;
                }
            }

            return new SolverState(
                removedMask,
                (int)model.CurrentDescriptor.Rank,
                model.NextBankIndex);
        }

        private static bool CanWinFrom(GameModel model, SolverState state, Dictionary<SolverState, bool> memo)
        {
            if (memo.TryGetValue(state, out var cached))
            {
                return cached;
            }

            if (IsWin(model, state))
            {
                memo[state] = true;
                return true;
            }

            var legalMoves = GetLegalMoves(model, state).ToList();

            if (legalMoves.Count == 0)
            {
                memo[state] = false;
                return false;
            }

            foreach (var move in legalMoves)
            {
                var next = ApplyMove(model, state, move);
                if (CanWinFrom(model, next, memo))
                {
                    memo[state] = true;
                    return true;
                }
            }

            memo[state] = false;
            return false;
        }

        private static bool IsWin(GameModel model, SolverState state)
        {
            for (var i = 0; i < model.AllCards.Count; i++)
            {
                if (!IsRemoved(state, i))
                {
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<HintMove> GetLegalMoves(GameModel model, SolverState state)
        {
            var currentRank = (CardRank)state.CurrentRankValue;

            for (var i = 0; i < model.AllCards.Count; i++)
            {
                var card = model.AllCards[i];

                if (IsRemoved(state, i))
                {
                    continue;
                }

                if (!IsExposed(model, state, card))
                {
                    continue;
                }

                if (currentRank.IsAdjacentCyclic(card.Descriptor.Rank))
                {
                    yield return new HintMove(HintMoveType.TakeCard, card);
                }
            }

            if (state.NextBankIndex < model.BankSequence.Count)
            {
                yield return new HintMove(HintMoveType.OpenBank);
            }
        }

        private static SolverState ApplyMove(GameModel model, SolverState state, HintMove move)
        {
            switch (move.Type)
            {
                case HintMoveType.TakeCard:
                    {
                        var cardIndex = model.AllCards.IndexOf(move.Card);
                        var newMask = state.RemovedMask | (1UL << cardIndex);

                        return new SolverState(
                            newMask,
                            (int)move.Card.Descriptor.Rank,
                            state.NextBankIndex);
                    }

                case HintMoveType.OpenBank:
                    {
                        var nextDescriptor = model.BankSequence[state.NextBankIndex];
                        return new SolverState(
                            state.RemovedMask,
                            (int)nextDescriptor.Rank,
                            state.NextBankIndex + 1);
                    }

                default:
                    return state;
            }
        }

        private static bool IsRemoved(SolverState state, int cardIndex)
        {
            return (state.RemovedMask & (1UL << cardIndex)) != 0;
        }

        private static bool IsExposed(GameModel model, SolverState state, CardModel card)
        {
            if (card.Parent == null)
            {
                return true;
            }

            var parentIndex = model.AllCards.IndexOf(card.Parent);
            return IsRemoved(state, parentIndex);
        }

        public static HintMove GetBestMove(GameModel model)
        {
            if (model == null || !model.IsPlaying)
            {
                return new HintMove(HintMoveType.None);
            }

            if (model.AllCards.Count > 64)
            {
                return new HintMove(HintMoveType.None);
            }

            var currentState = BuildCurrentState(model);
            var memo = new Dictionary<SolverState, bool>();

            var legalMoves = GetLegalMoves(model, currentState).ToList();

            foreach (var move in legalMoves)
            {
                var nextState = ApplyMove(model, currentState, move);

                if (CanWinFrom(model, nextState, memo))
                {
                    return move;
                }
            }

            return new HintMove(HintMoveType.None);
        }
    }
}