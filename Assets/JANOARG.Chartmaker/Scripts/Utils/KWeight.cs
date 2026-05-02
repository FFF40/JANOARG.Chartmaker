using UnityEngine;

namespace JANOARG.Chartmaker.Utils
{
    class KWeight
    {
        /// <summary>
        /// Apply standard K-weighting (ITU-R BS.1770-4) to a given array of float samples, in-place.
        /// </summary>
        /// <param name="samples">The array of float samples</param>
        /// <param name="sampleRate">The sampling rate (i.e. 44100 Hz)</param>
        public static void ApplyKWeightFilter(ref float[] samples, float sampleRate)
        {
            int n = samples.Length;

            // Standard K-weighting consists of two stages:
            // 1. Stage 1: High-shelf filter (Pre-filter)
            // 2. Stage 2: High-pass filter (RLB - Revised Low-frequency B-weighting)

            // Stage 1 Coefficients (High-shelf)
            var stage1 = GetHighShelfCoefficients(1500.0f, sampleRate, 1.5f, 1.0f);
            
            // Stage 2 Coefficients (High-pass)
            var stage2 = GetHighPassCoefficients(38.0f, sampleRate, 0.5f);

            // Buffers for filter states
            float x1 = 0, x2 = 0, y1 = 0, y2 = 0; // Stage 1
            float x3 = 0, x4 = 0, y3 = 0, y4 = 0; // Stage 2

            for (int i = 0; i < n; i++)
            {
                float input = samples[i];

                // Process Stage 1
                float filtered1 = stage1.b0 * input + stage1.b1 * x1 + stage1.b2 * x2 
                                - stage1.a1 * y1 - stage1.a2 * y2;
                x2 = x1; x1 = input;
                y2 = y1; y1 = filtered1;

                // Process Stage 2
                float filtered2 = stage2.b0 * filtered1 + stage2.b1 * x3 + stage2.b2 * x4 
                                - stage2.a1 * y3 - stage2.a2 * y4;
                x4 = x3; x3 = filtered1;
                y4 = y3; y3 = filtered2;

                samples[i] = filtered2;
            }
        }

        private static (float b0, float b1, float b2, float a1, float a2) GetHighShelfCoefficients(float targetFreq, float sampleRate, float gainDb, float quality)
        {
            float A = Mathf.Pow(10, gainDb / 40.0f);
            float w0 = 2 * Mathf.PI * targetFreq / sampleRate;
            float alpha = Mathf.Sin(w0) / (2 * quality);

            float cos_w0 = Mathf.Cos(w0);
            float sqrt_A = Mathf.Sqrt(A);
            float sqrtAmplitudeTwoAlpha = 2 * sqrt_A * alpha;
            float A_plus1 = A + 1;
            float A_minus1 = A - 1;

            float b0 = A * (A_plus1 + A_minus1 * cos_w0 + sqrtAmplitudeTwoAlpha);
            float b1 = -2 * A * (A_minus1 + A_plus1 * cos_w0);
            float b2 = A * (A_plus1 + A_minus1 * cos_w0 - sqrtAmplitudeTwoAlpha);
            float a0 = A_plus1 - A_minus1 * cos_w0 + sqrtAmplitudeTwoAlpha;
            float a1 = 2 * (A_minus1 - A_plus1 * cos_w0);
            float a2 = A_plus1 - A_minus1 * cos_w0 - sqrtAmplitudeTwoAlpha;

            return (b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0);
        }

        private static (float b0, float b1, float b2, float a1, float a2) GetHighPassCoefficients(float targetFreq, float sampleRate, float quality)
        {
            float w0 = 2 * Mathf.PI * targetFreq / sampleRate;
            float alpha = Mathf.Sin(w0) / (2 * quality);

            float cos_w0 = Mathf.Cos(w0);

            float b0 = (1 + cos_w0) / 2;
            float b1 = -(1 + cos_w0);
            float b2 = (1 + cos_w0) / 2;
            float a0 = 1 + alpha;
            float a1 = -2 * cos_w0;
            float a2 = 1 - alpha;

            return (b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0);
        }
    }
}