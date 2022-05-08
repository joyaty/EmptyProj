
using System.Collections.Generic;
using UnityEngine;

namespace Nt.Sound
{
    public class SpectrumData : ScriptableObject
    {
        [System.Serializable]
        public class SpectrumChunk
        {
            public float time;
            public float[] spectrumData;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<SpectrumChunk> allSpectrum;

        /// <summary>
        /// 音频样本时长
        /// </summary>
        public float length;

        /// <summary>
        /// 音频样本采样率
        /// </summary>
        public int sampleRate;
    }
}