using System;
using UnityEngine;

namespace Game.Minigames.Waveform
{
    public static class WaveformMath
    {
        public static float EvaluaSum(SineComponent[] cpn, float x)
        {
            float sum = 0;
            foreach(var c in cpn)
            {
                sum += c.Evaluate(x);
            }
            return sum;
        }

        public static float CalculateError(SineComponent[] target, SineComponent[] player, float xMin, float xMax, int resolution)
        {
            float totalError = 0f;
            for(int i = 0; i < resolution; i++)
            {
                float x = Mathf.Lerp(xMin, xMax, i / (float)(resolution - 1));
                float diff = EvaluaSum(target, x) - EvaluaSum(player, x);
                totalError += diff * diff; // RMS-style
            }

            return Mathf.Sqrt(totalError / resolution);
        }
    }

    public static class WaveformGenerator
    {
        public static SineComponent[] Generate(WaveformConfigSO config)
        {
            var res = new SineComponent[config.WaveComponentCount];
            for (int i = 0; i < config.WaveComponentCount; i++)
            {
                res[i] = new SineComponent
                {
                    Amplitude = UnityEngine.Random.Range(config.AmplitudeRange.x, config.AmplitudeRange.y),
                    Frequency = UnityEngine.Random.Range(config.FrequencyRange.x, config.FrequencyRange.y),
                    Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f)
                };
            }
            return res;
        }
    }
}