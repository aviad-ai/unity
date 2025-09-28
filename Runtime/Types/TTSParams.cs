using System;

namespace Aviad
{
    [Serializable]
    public class TTSParams
    {
        // Directory containing the TTS models/assets.
        public string directory;

        // Optional array of reference example identifiers (native expects int*).
        public int[] refExamples;

        // Callback signature for receiving generated audio samples.
        public delegate void TTSAudioCallback(float[] samples);

        public TTSParams(string directory, int[] refExamples = null)
        {
            this.directory = directory;
            this.refExamples = refExamples;
        }
    }
}
