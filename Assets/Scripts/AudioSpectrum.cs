
using System;
using System.Collections.Generic;
using UnityEngine;
using DSPLib;

[RequireComponent(typeof(AudioSource))]
public class AudioSpectrum : MonoBehaviour
{
    private AudioSource m_AudioSource;

    /// <summary>
    /// 采样1024个频段
    /// </summary>
    private int m_SampleCount = 512;
    /// <summary>
    /// 采样频段间隔，48000Hz => 23.4Hz / 44100Hz => 21.5Hz
    /// </summary>
    private float m_HertzPerBin;

    /// <summary>
    /// 频段区间数量
    /// </summary>
    private int m_RangeCount = 20;
    /// <summary>
    /// 绘制频段分布
    /// </summary>
    private int[] m_HertzRanges;
    /// <summary>
    /// 各个频段区间代表的Cube
    /// </summary>
    private GameObject[] m_FirstCubes;

    private GameObject[] m_SecondCubes;

    /// <summary>
    /// 样本数据
    /// </summary>
    private float[] m_PreProcessSampleData;
    /// <summary>
    /// 采样频率
    /// </summary>
    private int m_SampleRate;

    private Dictionary<float, float[]> m_PreprocessSpectrumData;

    public bool showRealTime = true;
    public bool showPreProcess = true;


    void Awake()
    {
        Application.targetFrameRate = 30;
        m_AudioSource = GetComponent<AudioSource>();

        Debug.Log("样本数:" + m_AudioSource.clip.samples);
        Debug.Log("采样频率:" + m_AudioSource.clip.frequency);
        Debug.Log("时长:" + (float)m_AudioSource.clip.samples / (float)m_AudioSource.clip.frequency + ", " + m_AudioSource.clip.length);
        Debug.Log("频道数:" + m_AudioSource.clip.channels);
        Debug.Log("输出采样率:" + AudioSettings.outputSampleRate);

        float[] clipData = new float[m_AudioSource.clip.samples * m_AudioSource.clip.channels];
        m_AudioSource.clip.GetData(clipData, 0);

        // 采样的区间值
        m_HertzPerBin = (float)AudioSettings.outputSampleRate / 2f / m_SampleCount;

        /*
        0 20 - 60 - 50  
        1 60 - 80 - 69
        2 80 - 110 - 94
        3 110 - 150 - 129
        4 150 - 200 - 176
        5 200 - 280 - 241
        6 280 - 380 - 331
        7 380 - 520 - 453
        8 520 - 720 - 620
        9 720 - 1000 - 850
        10 1000 - 1400 - 1200
        11 1400 - 1800 - 1600
        12 1800 - 2600 - 2200
        14 2600 - 3400 - 3000
        15 3400 - 4800 - 4100
        16 4800 - 6400 - 5600
        17 6400 - 9000 - 7700
        18 9000 - 12000 - 11000
        19 12000 - 16000 - 14000
        20 16000 - 20000 - 20000
        */
        m_HertzRanges = new int[] { 50, 69, 94, 129, 176, 241, 331, 453, 620, 850, 1200, 1600, 2200, 3000, 4100, 5600, 7700, 11000, 14000, 20000 };
        m_FirstCubes = new GameObject[m_RangeCount];
        GameObject firstCube = GameObject.Find("FirstCube");
        for (int i = 0; i < m_RangeCount; ++i)
        {
            Transform cube = firstCube.transform.Find("Cube_" + (i + 1));
            m_FirstCubes[i] = cube.gameObject;
        }

        m_SecondCubes = new GameObject[m_RangeCount];
        GameObject secondCube = GameObject.Find("SecondCube");
        for (int i = 0; i < m_RangeCount; ++i)
        {
            Transform cube = secondCube.transform.Find("Cube_" + (i + 1));
            m_SecondCubes[i] = cube.gameObject;
        }

        GetSampleDataFromAudioClip();
        AnalyzeSampleData();
        float value = Mathf.Pow(2, 0) * 2;
    }

    void Update()
    {
        if (m_AudioSource.isPlaying)
        {
            Debug.Log("" + m_AudioSource.time + ", " + m_AudioSource.timeSamples);
            if (showRealTime)
            {
                ShowFirstSpectrum();
            }
            if (showPreProcess)
            {
                ShowSecondSpectrum(m_AudioSource.time);
            }
            // float[] outputData = new float[1024];
            // m_AudioSource.GetOutputData(outputData, 0);

            // float value = m_PreProcessSampleData[m_AudioSource.timeSamples];
            // string debugContext = "";
            // foreach(float outputValue in outputData)
            // {
            //     debugContext += outputValue + ", ";
            // }
            // Debug.Log("实时输出值" + debugContext);
            // Debug.Log("预处理样本值" + value);
        }
    }

    private void ShowFirstSpectrum()
    {
        float[] spectrum = new float[m_SampleCount];
        m_AudioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        float curHertz = 0;
        int rangeIndex = 0;
        float sum = 0;
        int count = 0;
        float[] showSpectrum = new float[m_RangeCount];
        for (int i = 0; i < spectrum.Length; ++i)
        {
            if (rangeIndex < m_RangeCount && curHertz > m_HertzRanges[rangeIndex])
            {
                showSpectrum[rangeIndex] = sum / count;
                sum = 0;
                count = 0;
                ++rangeIndex;
            }
            sum += spectrum[i];
            ++count;
            curHertz += m_HertzPerBin;
        }

        for (int i = 0; i < m_RangeCount; ++i)
        {
            GameObject cube = m_FirstCubes[i];
            Vector3 scaleValue = cube.transform.localScale;
            cube.transform.localScale = new Vector3(scaleValue.x, showSpectrum[i] * 20f, scaleValue.z);
        }
    }

    private void ShowSecondSpectrum(float time)
    {
        float[] spectrum = GetPreProcessSpectrum(time);

        float curHertz = 0;
        int rangeIndex = 0;
        float sum = 0;
        int count = 0;
        float[] showSpectrum = new float[m_RangeCount];
        for (int i = 0; i < spectrum.Length; ++i)
        {
            if (rangeIndex < m_RangeCount && curHertz > m_HertzRanges[rangeIndex])
            {
                showSpectrum[rangeIndex] = sum / count;
                sum = 0;
                count = 0;
                ++rangeIndex;
            }
            sum += spectrum[i];
            ++count;
            curHertz += m_HertzPerBin;
        }

        for (int i = 0; i < m_RangeCount; ++i)
        {
            GameObject cube = m_SecondCubes[i];
            Vector3 scaleValue = cube.transform.localScale;
            cube.transform.localScale = new Vector3(scaleValue.x, showSpectrum[i] * 20f, scaleValue.z);
        }
    }

    void OnEnable()
    {
        m_AudioSource.Play();
    }

    private void GetSampleDataFromAudioClip()
    {
        // 音频样本数(单声道)
        int sampleNum = m_AudioSource.clip.samples;
        // 音频样本声道数
        int channelNum = m_AudioSource.clip.channels;
        // 音频样本时长
        float clipLength = m_AudioSource.clip.length;
        // 音频样本采样频率
        m_SampleRate = m_AudioSource.clip.frequency;

        // 获取音频样本数据，可能是双声道[L,R,L,R,L,R,.....]
        float[] multiSampleDatas = new float[sampleNum * channelNum];
        m_AudioSource.clip.GetData(multiSampleDatas, 0);
        m_PreProcessSampleData = new float[sampleNum];
        if (channelNum == 1)
        {
            // 原本就是单声道，直接拷贝数据
            multiSampleDatas.CopyTo(m_PreProcessSampleData, 0);
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
                    m_PreProcessSampleData[indexProcessed] = combineChangeAverage / channelNum;
                    ++indexProcessed;
                    combineChangeAverage = 0f;
                }
            }
        }
    }

    private void AnalyzeSampleData()
    {
        m_PreprocessSpectrumData = new Dictionary<float, float[]>();
        int sampleNum = m_SampleCount * 2;
        int iterations = m_PreProcessSampleData.Length / sampleNum;
        FFT fft = new FFT();
        fft.Initialize((uint)sampleNum);

        Debug.Log(string.Format("Processing {0} time domain samples for FFT", iterations));
        double[] sampleChunk = new double[sampleNum];
        for (int i = 0; i < iterations; ++i)
        {
            // 搜集 1024 样本数据
            Array.Copy(m_PreProcessSampleData, i * sampleNum, sampleChunk, 0, sampleNum);

            double[] windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.Hanning, (uint)sampleNum);
            double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);
            double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

            System.Numerics.Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
            double[] scaledFFTSepctrum = DSPLib.DSP.ConvertComplex.ToMagnitude(fftSpectrum);
            scaledFFTSepctrum = DSP.Math.Multiply(scaledFFTSepctrum, scaleFactor);

            float curAudioTime = getTimeFromIndex(i * sampleNum);
            m_PreprocessSpectrumData.Add(curAudioTime, Array.ConvertAll(scaledFFTSepctrum, value => (float)value));
        }

        Debug.Log("Spectrum analysis done");
    }

    private float getTimeFromIndex(int index)
    {
        return ((1f / (float)this.m_SampleRate) * index);
    }

    private float[] GetPreProcessSpectrum(float time)
    {
        float[] spectrumData = m_PreprocessSpectrumData[0];
        foreach (var iter in m_PreprocessSpectrumData)
        {
            if (iter.Key > time)
            {
                break;
            }
            spectrumData = iter.Value;
        }
        return spectrumData;
    }

}
