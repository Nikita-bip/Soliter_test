using Assets.Scripts.Models;
using Assets.Scripts.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Controllers
{
    public static class LayoutAnalyzer
    {
        public static GameModel Analyze(IReadOnlyList<CardView> cardViews, float pileGroupingThreshold)
        {
            if (cardViews == null || cardViews.Count == 0)
            {
                throw new ArgumentException("No scene cards found.", nameof(cardViews));
            }

            var groupedViews = GroupIntoPiles(cardViews, pileGroupingThreshold);

            var model = new GameModel();
            var idCounter = 0;

            for (var pileIndex = 0; pileIndex < groupedViews.Count; pileIndex++)
            {
                var pile = new PileModel { Index = pileIndex };
                var orderedViews = OrderTopToBottom(groupedViews[pileIndex]);

                for (var i = 0; i < orderedViews.Count; i++)
                {
                    var view = orderedViews[i];
                    view.CacheInitialPosition();

                    var card = new CardModel
                    {
                        Id = idCounter++,
                        PileIndex = pileIndex,
                        IndexInPile = i,
                        View = view,
                        InitialAnchoredPosition = view.InitialAnchoredPosition,
                    };

                    pile.CardsTopToBottom.Add(card);
                    model.AllCards.Add(card);
                }

                for (var i = 0; i < pile.CardsTopToBottom.Count; i++)
                {
                    var card = pile.CardsTopToBottom[i];
                    card.Parent = i > 0 ? pile.CardsTopToBottom[i - 1] : null;
                    card.Child = i < pile.CardsTopToBottom.Count - 1 ? pile.CardsTopToBottom[i + 1] : null;
                }

                model.Piles.Add(pile);
            }

            return model;
        }

        private static List<List<CardView>> GroupIntoPiles(IReadOnlyList<CardView> cardViews, float pileGroupingThreshold)
        {
            var sortedByX = cardViews
                .OrderBy(view => view.RectTransform.anchoredPosition.x)
                .ToList();

            var groupedViews = new List<List<CardView>>();
            var currentGroup = new List<CardView>();
            float? currentCenterX = null;

            foreach (var view in sortedByX)
            {
                var x = view.RectTransform.anchoredPosition.x;

                if (currentGroup.Count == 0)
                {
                    currentGroup.Add(view);
                    currentCenterX = x;
                    continue;
                }

                if (Mathf.Abs(x - currentCenterX.Value) <= pileGroupingThreshold)
                {
                    currentGroup.Add(view);
                    currentCenterX = currentGroup.Average(v => v.RectTransform.anchoredPosition.x);
                }
                else
                {
                    groupedViews.Add(currentGroup);
                    currentGroup = new List<CardView> { view };
                    currentCenterX = x;
                }
            }

            if (currentGroup.Count > 0)
            {
                groupedViews.Add(currentGroup);
            }

            return groupedViews;
        }

        private static List<CardView> OrderTopToBottom(IReadOnlyList<CardView> pileViews)
        {
            if (pileViews.Count <= 1)
            {
                return pileViews.ToList();
            }

            var remaining = pileViews
                .OrderByDescending(v => v.RectTransform.position.y)
                .ToList();

            var ordered = new List<CardView>(remaining.Count);

            var current = remaining[0];
            ordered.Add(current);
            remaining.RemoveAt(0);

            while (remaining.Count > 0)
            {
                CardView bestNext = null;
                var bestScore = float.MinValue;

                foreach (var candidate in remaining)
                {
                    var score = GetChainScore(current, candidate);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestNext = candidate;
                    }
                }

                if (bestNext == null)
                {
                    bestNext = remaining
                        .OrderByDescending(v => v.RectTransform.position.y)
                        .First();
                }

                ordered.Add(bestNext);
                remaining.Remove(bestNext);
                current = bestNext;
            }

            return ordered;
        }

        private static float GetChainScore(CardView upper, CardView lower)
        {
            var upperY = upper.RectTransform.position.y;
            var lowerY = lower.RectTransform.position.y;

            if (lowerY > upperY + 0.01f)
            {
                return float.MinValue;
            }

            var upperRect = GetWorldRect(upper.RectTransform);
            var lowerRect = GetWorldRect(lower.RectTransform);

            var overlapArea = GetOverlapArea(upperRect, lowerRect);
            var verticalGap = upperY - lowerY;
            var horizontalOffset = Mathf.Abs(upperRect.center.x - lowerRect.center.x);

            return overlapArea * 1000f - verticalGap * 10f - horizontalOffset;
        }

        private static Rect GetWorldRect(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            return Rect.MinMaxRect(
                corners[0].x,
                corners[0].y,
                corners[2].x,
                corners[2].y);
        }

        private static float GetOverlapArea(Rect a, Rect b)
        {
            var overlapX = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
            var overlapY = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
            return overlapX * overlapY;
        }
    }
}