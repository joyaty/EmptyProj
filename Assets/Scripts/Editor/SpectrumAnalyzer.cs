

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DSPLib;

namespace Nt.Sound.Editor
{
    /// <summary>
    /// 音频频谱分析器
    /// </summary>
    public static class SpectrumAnalyzer
    {
        [MenuItem("Assets/Audio/Generate Spectrum")]
        public static void AnalysisAudioClip()
        {
            AudioClip audioClip = Selection.activeObject as AudioClip;
            if (audioClip == null)
            {
                Debug.LogError("当前选中不是音频频段，无法分析频谱数据");
                return;
            }

            // 获取音频样本初始数据
            GetSampleDataFromAudioClip(audioClip, out float[] processSampleDatas);
            // 分析样本频谱数据
            AnalysisSpectrumData(processSampleDatas, audioClip.frequency, 1024, out Dictionary<float, float[]> spectrumData);
            // 保存频谱数据
            SpectrumData saveSpectrumData = ScriptableObject.CreateInstance<SpectrumData>();
            saveSpectrumData.sampleRate = audioClip.frequency;
            saveSpectrumData.length = audioClip.length;
            saveSpectrumData.allSpectrum = new List<SpectrumData.SpectrumChunk>(spectrumData.Count);
            int index = 0;
            foreach (var iter in spectrumData)
            {
                saveSpectrumData.allSpectrum.Add(new SpectrumData.SpectrumChunk()
                {
                    time = iter.Key,
                    spectrumData = iter.Value,
                });
                ++index;
            }
            saveSpectrumData.name = audioClip.name;
            string path = AssetDatabase.GetAssetPath(audioClip);
            path = path.Substring(0, path.IndexOf(audioClip.name));
            path += saveSpectrumData.name + ".asset";
            Debug.Log("path=" + path);
            AssetDatabase.CreateAsset(saveSpectrumData, path);
        }

        /// <summary>
        /// 获取音频样本数据
        /// </summary>
        /// <param name="audioClip">音频片段</param>
        /// <param name="processedSampleDatas">音频样本数据</param>
        private static bool GetSampleDataFromAudioClip(AudioClip audioClip, out float[] processedSampleDatas)
        {
            // 音频样本数(单声道)
            int sampleNum = audioClip.samples;
            // 音频样本声道数
            int channelNum = audioClip.channels;
            // 获取音频样本数据，可能是双声道[L,R,L,R,L,R,.....]
            float[] multiSampleDatas = new float[sampleNum * channelNum];
            audioClip.GetData(multiSampleDatas, 0);
            processedSampleDatas = new float[sampleNum];
            if (channelNum == 1)
            {
                // 原本就是单声道，直接拷贝数据
                multiSampleDatas.CopyTo(processedSampleDatas, 0);
            }
            else
            {
                // stereo处理为单声道样本数据，双声道去两个声道样本平均值，合并为mono
                int indexProcessed = 0;
                float combineChangeAverage = 0f;
                for (int i = 0; i < multiSampleDatas.Length; ++i)
                {
                    combineChangeAverage += multiSampleDatas[i];
                    if ((i + 1) % channelNum == 0)
                    {
                        processedSampleDatas[indexProcessed] = combineChangeAverage / channelNum;
                        ++indexProcessed;
                        combineChangeAverage = 0f;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 分析音频样本的频谱数据
        /// </summary>
        /// <param name="processedSampleDatas">音频样本数据</param>
        /// <param name="sampleRate">样本采样率</param>
        /// <param name="sampleNum">频谱采样长度</param>
        /// <param name="spectrumDatas">输出的频谱数据</param>
        private static void AnalysisSpectrumData(in float[] processedSampleDatas, int sampleRate, int sampleLength, out Dictionary<float, float[]> spectrumDatas)
        {
            spectrumDatas = new Dictionary<float, float[]>();
            int iterations = processedSampleDatas.Length / sampleLength;
            FFT fft = new FFT();
            fft.Initialize((uint)sampleLength);

            Debug.Log(string.Format("Processing {0} time domain samples for FFT", iterations));
            double[] sampleChunk = new double[sampleLength];
            for (int i = 0; i < iterations; ++i)
            {
                Array.Copy(processedSampleDatas, i * sampleLength, sampleChunk, 0, sampleLength);

                double[] windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.Hanning, (uint)sampleLength);
                double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);
                double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

                System.Numerics.Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
                double[] scaledFFTSepctrum = DSPLib.DSP.ConvertComplex.ToMagnitude(fftSpectrum);
                scaledFFTSepctrum = DSP.Math.Multiply(scaledFFTSepctrum, scaleFactor);

                float curAudioTime = (1f / (float)sampleRate) * (i * sampleLength);
                spectrumDatas.Add(curAudioTime, Array.ConvertAll(scaledFFTSepctrum, value => (float)value));
            }

            Debug.Log("Spectrum analysis done");
        }
    }
}

#endif
