using UnityEngine;

namespace TestTask.Solitaire.Config
{
    [System.Serializable]
    public sealed class SolitaireGeneratorSettings
    {
        [Header("Layout")]
        [Min(1f)] public float pileGroupingThreshold = 120f;

        [Header("Generator")]
        [Range(0f, 1f)] public float upwardChance = 0.65f;
        [Range(0f, 1f)] public float directionFlipChance = 0.15f;
        [Min(2)] public int minCombinationLength = 2;
        [Min(2)] public int maxCombinationLength = 7;

        [Header("Animation")]
        [Min(0.01f)] public float moveDuration = 0.22f;
        [Min(0.01f)] public float flipDuration = 0.16f;
        [Min(0.01f)] public float revealDelay = 0.04f;
    }
}
