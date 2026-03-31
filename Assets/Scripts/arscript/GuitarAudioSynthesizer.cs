using UnityEngine;

namespace DetectiveGame.HandTracking
{
    /// <summary>
    /// Generates procedural audio (Sine Waves) for the Guitar AR system.
    /// Simulates a simple synthesizer that can play frequencies based on active chords.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GuitarAudioSynthesizer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float gain = 0.0f; // Current volume
        [SerializeField] private float decayRate = 2.0f; // How fast sound fades
        [SerializeField] private float maxVolume = 0.5f;

        private double phase;
        private double increment;
        private float currentFrequency = 440f; // A4 default
        private float samplingFrequency = 48000f;
        private bool isMuted = true;

        private void Start()
        {
            samplingFrequency = AudioSettings.outputSampleRate;
        }

        private void Update()
        {
            // Apply decay for "pluck" effect
            if (gain > 0)
            {
                gain -= decayRate * Time.deltaTime;
                if (gain < 0) gain = 0;
            }
        }

        /// <summary>
        /// Called by Unity's Audio Thread to generate sound.
        /// </summary>
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (isMuted && gain <= 0.001f) return;

            increment = currentFrequency * 2.0 * Mathf.PI / samplingFrequency;

            for (int i = 0; i < data.Length; i += channels)
            {
                phase += increment;
                
                // Keep phase within 0-2PI to prevent overflow issues over long time
                if (phase > 2 * Mathf.PI) phase -= 2 * Mathf.PI;

                // Basic Sine Wave
                float signal = (float)Mathf.Sin((float)phase);

                // Add some harmonics for a slightly richer "guitar-ish" (but still synth) tone
                // signal += 0.5f * (float)Mathf.Sin((float)phase * 2); // Octave

                // Apply Gain
                signal *= gain;

                // Write to all channels
                for (int c = 0; c < channels; c++)
                {
                    data[i + c] = signal;
                }
            }
        }

        public void PlayChord(GuitarChord chord)
        {
            // Map chords to frequencies
            // Simple mapping for now
            switch (chord)
            {
                case GuitarChord.A_Major:
                    currentFrequency = 440.0f; // A4
                    break;
                case GuitarChord.B_Minor:
                    currentFrequency = 493.88f; // B4
                    break;
                case GuitarChord.C_Major:
                    currentFrequency = 523.25f; // C5
                    break;
                case GuitarChord.D_Major:
                    currentFrequency = 587.33f; // D5
                    break;
                case GuitarChord.E_Major:
                    currentFrequency = 659.25f; // E5
                    break;
                case GuitarChord.F_Major:
                    currentFrequency = 698.46f; // F5
                    break;
                case GuitarChord.G_Major:
                    currentFrequency = 783.99f; // G5
                    break;
                case GuitarChord.None:
                default:
                    // Don't change frequency, just ensure we mute effectively if needed
                    // or maybe drop pitch? let's stick to last note but muted
                    break;
            }
        }

        public void Strum()
        {
            // "Pluck" the string -> Reset gain to max
            gain = maxVolume;
            isMuted = false;
        }

        public void Stop()
        {
            isMuted = true;
            gain = 0;
        }
    }
}
