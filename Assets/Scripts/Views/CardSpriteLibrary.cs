using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Models;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.Views
{
    [CreateAssetMenu(menuName = "TestTask/Solitaire/Card Sprite Library", fileName = "CardSpriteLibrary")]
    public sealed class CardSpriteLibrary : ScriptableObject
    {
        [Serializable]
        private struct CardSpriteEntry
        {
            public CardRank rank;
            public CardSuit suit;
            public Sprite sprite;
        }

        [Header("Manual")]
        [SerializeField] private Sprite cardBack;
        [SerializeField] private List<CardSpriteEntry> faceSprites = new();

        [Header("Auto Fill")]
        [SerializeField] private UnityEngine.Object spritesFolder;

        private Dictionary<(CardRank, CardSuit), Sprite> _cache;

        public Sprite CardBack => cardBack;

        public Sprite GetFace(CardDescriptor descriptor)
        {
            if (_cache == null)
            {
                _cache = new Dictionary<(CardRank, CardSuit), Sprite>();

                foreach (var entry in faceSprites)
                {
                    _cache[(entry.rank, entry.suit)] = entry.sprite;
                }
            }

            return _cache.TryGetValue((descriptor.Rank, descriptor.Suit), out var sprite)
                ? sprite
                : cardBack;
        }

#if UNITY_EDITOR
        public void AutoFillFromFolder()
        {
            _cache = null;
            faceSprites.Clear();

            if (spritesFolder == null)
            {
                Debug.LogError("CardSpriteLibrary: не выбрана папка со спрайтами.");
                return;
            }

            var folderPath = AssetDatabase.GetAssetPath(spritesFolder);
            if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogError($"CardSpriteLibrary: объект '{spritesFolder.name}' не является папкой.");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            var tempEntries = new List<CardSpriteEntry>();

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                    .OfType<Sprite>()
                    .ToArray();

                if (sprites.Length == 0)
                {
                    var singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (singleSprite != null)
                    {
                        sprites = new[] { singleSprite };
                    }
                }

                foreach (var sprite in sprites)
                {
                    if (sprite == null)
                    {
                        continue;
                    }

                    if (TryParseCardSpriteName(sprite.name, out var rank, out var suit))
                    {
                        tempEntries.Add(new CardSpriteEntry
                        {
                            rank = rank,
                            suit = suit,
                            sprite = sprite
                        });
                    }
                    else if (IsBackSpriteName(sprite.name))
                    {
                        cardBack = sprite;
                    }
                }
            }

            faceSprites = tempEntries
                .GroupBy(x => (x.rank, x.suit))
                .Select(g => g.Last())
                .OrderBy(x => x.suit)
                .ThenBy(x => x.rank)
                .ToList();

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"CardSpriteLibrary: заполнено {faceSprites.Count} карт.");
        }
#endif

        private static bool TryParseCardSpriteName(string spriteName, out CardRank rank, out CardSuit suit)
        {
            rank = default;
            suit = default;

            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return false;
            }

            var normalized = spriteName.Trim();

            var separator = "_of_";
            var index = normalized.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            var rankPart = normalized.Substring(0, index).Trim();
            var suitPart = normalized.Substring(index + separator.Length).Trim();

            if (!TryParseRank(rankPart, out rank))
            {
                return false;
            }

            if (!TryParseSuit(suitPart, out suit))
            {
                return false;
            }

            return true;
        }

        private static bool TryParseRank(string value, out CardRank rank)
        {
            rank = default;

            switch (value.Trim().ToUpperInvariant())
            {
                case "A":
                case "ACE":
                    rank = CardRank.Ace;
                    return true;

                case "2":
                    rank = CardRank.Two;
                    return true;

                case "3":
                    rank = CardRank.Three;
                    return true;

                case "4":
                    rank = CardRank.Four;
                    return true;

                case "5":
                    rank = CardRank.Five;
                    return true;

                case "6":
                    rank = CardRank.Six;
                    return true;

                case "7":
                    rank = CardRank.Seven;
                    return true;

                case "8":
                    rank = CardRank.Eight;
                    return true;

                case "9":
                    rank = CardRank.Nine;
                    return true;

                case "10":
                    rank = CardRank.Ten;
                    return true;

                case "J":
                case "JACK":
                    rank = CardRank.Jack;
                    return true;

                case "Q":
                case "QUEEN":
                    rank = CardRank.Queen;
                    return true;

                case "K":
                case "KING":
                    rank = CardRank.King;
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryParseSuit(string value, out CardSuit suit)
        {
            suit = default;

            switch (value.Trim().ToUpperInvariant())
            {
                case "CLUB":
                case "CLUBS":
                    suit = CardSuit.Clubs;
                    return true;

                case "DIAMOND":
                case "DIAMONDS":
                    suit = CardSuit.Diamonds;
                    return true;

                case "HEART":
                case "HEARTS":
                    suit = CardSuit.Hearts;
                    return true;

                case "SPADE":
                case "SPADES":
                    suit = CardSuit.Spades;
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsBackSpriteName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var upper = value.Trim().ToUpperInvariant();
            return upper == "BACK" || upper == "CARD_BACK" || upper == "CARD BACK";
        }
    }
}