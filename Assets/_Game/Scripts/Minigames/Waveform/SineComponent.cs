using System;
using UnityEngine;

namespace Game.Minigames.Waveform
{
    [Serializable]
    public struct SineComponent
    {
        public float Amplitude;
        public float Frequency;
        public float Phase;

        public float Evaluate(float x)
        {
            return Amplitude * Mathf.Sin(Frequency * x + Phase);
        }
    }
}