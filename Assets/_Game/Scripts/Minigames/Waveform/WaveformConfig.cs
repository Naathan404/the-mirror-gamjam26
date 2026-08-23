using UnityEngine;

namespace Game.Minigames.Waveform
{
    [CreateAssetMenu(fileName = "WaveformConfig", menuName = "Game/Minigames/Waveform Config")]
    public class WaveformConfigSO : ScriptableObject
    {
        [Header("Wave Composition")]
        public int WaveComponentCount = 3;
        public float DomainHalfWidth = 6f;

        [Header("Parameter Ranges")]
        public Vector2 AmplitudeRange = new Vector2(0.2f, 1.5f);
        public Vector2 FrequencyRange = new Vector2(0.5f, 4f);

        [Header("Input")]
        public float AmplitudeStep = 0.1f;
        public float FrequencyStep = 0.25f;

        [Header("Comparison")]
        public float MatchErrorTolerance = 0.15f;
        public int SampleResolution = 100;

        [Header("Mistake Handling")]
        public int MaxMistakes = 2;
        public bool ResetDialsOnRegenerate = true;
    }
}