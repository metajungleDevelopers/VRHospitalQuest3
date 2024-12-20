﻿using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.IO;

namespace FMSolution.FMETP
{
    public static class FMCoreTools_V3
    {
        /// <summary>
        /// the simple way to get yield return value from coroutine
        /// yield return FMCoreTools.RunCOR<byte[]>(YourCOR, (output) => YourVariableForOutput = output);
        /// </summary>
        public static IEnumerator RunCOR<T>(IEnumerator target, Action<T> output)
        {
            object result = null;
            while (target.MoveNext())
            {
                result = target.Current;
                yield return result;
            }
            output((T)result);
        }

        /// <summary>
        /// the simple way to get yield return value from coroutine
        /// example script:
        /// yield return FMCoreTools.RunCOR<byte[]>(FMCoreTools.FMZippedByteCOR(dataByte), (output) => dataByte = output);
        /// </summary>
        public static IEnumerator FMZippedByteCOR(byte[] _inputByte)
        {
            byte[] _unzippedByte = new byte[0];
            if (Loom.numThreads < Loom.maxThreads - 1)
            {
                //reserve at least one thread for other purpose
                //need to clone a buffer for multi-threading
                byte[] _bufferByte = new byte[_inputByte.Length];
                Buffer.BlockCopy(_inputByte, 0, _bufferByte, 0, _inputByte.Length);

                bool AsyncEncoding = true;
                Loom.RunAsync(() =>
                {
                    try { _unzippedByte = _bufferByte.FMZipBytes(); } catch { }
                    AsyncEncoding = false;
                });
                while (AsyncEncoding) yield return null;
            }
            else { try { _unzippedByte = _inputByte.FMZipBytes(); } catch { } }
            yield return _unzippedByte;
        }

        /// <summary>
        /// the simple way to get yield return value from coroutine
        /// example script:
        /// yield return FMCoreTools.RunCOR<byte[]>(FMCoreTools.FMUnzippedByteCOR(inputByteData), (output) => inputByteData = output);
        /// </summary>
        public static IEnumerator FMUnzippedByteCOR(byte[] _inputByte)
        {
            byte[] _unzippedByte = new byte[0];
            //reserve at least one thread for other purpose
            if (Loom.numThreads < Loom.maxThreads - 1)
            {
                //reserve at least one thread for other purpose
                //need to clone a buffer for multi-threading
                byte[] _bufferByte = new byte[_inputByte.Length];
                Buffer.BlockCopy(_inputByte, 0, _bufferByte, 0, _inputByte.Length);

                bool AsyncEncoding = true;
                Loom.RunAsync(() =>
                {
                    try { _unzippedByte = _bufferByte.FMUnzipBytes(); } catch { }
                    AsyncEncoding = false;
                });
                while (AsyncEncoding) yield return null;
            }
            else { try { _unzippedByte = _inputByte.FMUnzipBytes(); } catch { } }
            yield return _unzippedByte;
        }

        /// <summary>
        /// convert byte[] to float[]
        /// </summary>
        public static float[] ToFloatArray(byte[] byteArray)
        {
            int len = byteArray.Length / 2;
            float[] floatArray = new float[len];
            for (int i = 0; i < byteArray.Length; i += 2)
            {
                floatArray[i / 2] = ((float)BitConverter.ToInt16(byteArray, i)) / 32767f;
            }
            return floatArray;
        }

        /// <summary>
        /// convert float to Int16 space
        /// </summary>
        public static Int16 FloatToInt16(float inputFloat)
        {
            inputFloat *= 32767;
            if (inputFloat < -32768) inputFloat = -32768;
            if (inputFloat > 32767) inputFloat = 32767;
            return Convert.ToInt16(inputFloat);
        }

        /// <summary>
        /// compare two int values, return true if they are similar referring to the sizeThreshold
        /// </summary>
        public static bool CheckSimilarSize(int inputByteLength1, int inputByteLength2, int sizeThreshold)
        {
            float diff = Mathf.Abs(inputByteLength1 - inputByteLength2);
            return diff < sizeThreshold;
        }
    }

    [AddComponentMenu("FMETP/Mapper/GameViewDecoder")]
    public class GameViewDecoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowSettings = true;
        public bool EditorShowDecoded = true;
        public bool EditorShowDesktopFrameInfo = false;
        public bool EditorShowPairing = true;
        #endregion

        public static GameViewDecoder InitAndCreate(
            GameObject go,
            Material inputMatColorAdjustment,

            bool inputFastMode,
            bool inputAsyncMode,
            float inputDecoderDelay,
            bool inputMono,
            FilterMode inputDecodedFilterMode,
            TextureWrapMode inputDecodedWrapMode,
            float inputSharpen,
            float inputDeNoise,
            GameViewPreviewType inputPreviewType,
            RawImage inputPreviewRawImage,
            MeshRenderer inputPreviewMeshRenderer,
            UInt16 inputlabel
            )
        {
            GameViewDecoder _decoder = go.AddComponent<GameViewDecoder>();

            _decoder.MatColorAdjustment = inputMatColorAdjustment;

            _decoder.FastMode = inputFastMode;
            _decoder.AsyncMode = inputAsyncMode;

            _decoder.DecoderDelay = inputDecoderDelay;
            _decoder.Mono = inputMono;

            _decoder.DecodedFilterMode = inputDecodedFilterMode;
            _decoder.DecodedWrapMode = inputDecodedWrapMode;

            _decoder.Sharpen = inputSharpen;
            _decoder.DeNoise = inputDeNoise;

            _decoder.PreviewType = inputPreviewType;
            _decoder.PreviewRawImage = inputPreviewRawImage;
            _decoder.PreviewMeshRenderer = inputPreviewMeshRenderer;

            _decoder.label = inputlabel;
            return _decoder;
        }

        public bool FastMode = false;
        public bool AsyncMode = false;

        [Range(0f, 10f)]
        public float DecoderDelay = 0f;
        private float DecoderDelay_old = 0f;

        public Texture ReceivedTexture { get { return (ColorReductionLevel > 0 || DeNoise > 0 || Sharpen > 0 ? (Texture)ReceivedRenderTexture : (Texture)ReceivedTexture2D); } }
        public Texture2D ReceivedTexture2D;
        public RenderTexture ReceivedRenderTexture;
        public int ColorReductionLevel = 0;

        private bool isDesktopFrame = false;
        private Rect fmDesktopFrameRect = new Rect(0, 0, 0, 0);
        private float fmDesktopMonitorScaling = 1f;

        public bool IsDesktopFrame { get { return isDesktopFrame; } }
        private FMDesktopSystemOS fmDesktopSystemOS = FMDesktopSystemOS.NULL;
        /// <summary>
        /// Return original Desktop Monitor Frame OffsetX, OffsetY, Width, Height when detected, otherwise return zero value;
        /// </summary>
        public Rect GetFMDesktopFrameRect { get { return isDesktopFrame ? fmDesktopFrameRect : new Rect(0, 0, 0, 0); } }
        public float GetFMDesktopMonitorScaling { get { return isDesktopFrame ? fmDesktopMonitorScaling : 1f; } }
        public FMDesktopSystemOS GetFMDesktopSystemOS { get { return fmDesktopSystemOS; } }

        public GameViewPreviewType PreviewType = GameViewPreviewType.None;
        public RawImage PreviewRawImage;
        public MeshRenderer PreviewMeshRenderer;

        /// <summary>
        /// OnReceivedTextureEvent() will be invoked when the received new texture is ready
        /// </summary>
        public UnityEventTexture OnReceivedTextureEvent = new UnityEventTexture();
        /// <summary>
        /// OnReceivedDesktopFrameRectEvent will be invoked when the received frame is detected as Desktop Monitor frame, and it will be invoke once after OnReceivedTextureEvent().
        /// </summary>
        public UnityEventRect OnReceivedDesktopFrameRectEvent = new UnityEventRect();

        [Tooltip("Mono return texture format R8, otherwise it's RGB24 by default")]
        public bool Mono = false;
        [Range(0f, 1f)] public float Sharpen = 0f;
        [Range(0f, 1f)] public float DeNoise = 0f;
        public FilterMode DecodedFilterMode = FilterMode.Bilinear;
        public TextureWrapMode DecodedWrapMode = TextureWrapMode.Clamp;

        [HideInInspector] public Material MatColorAdjustment;
        private void AssignMaterials(bool _override = false)
        {
            if (_override)
            {
                try { MatColorAdjustment = new Material(Shader.Find("Hidden/FMETPColorAdjustment")); } catch (Exception e) { Debug.LogWarning(e); }
            }
            else
            {
                if (MatColorAdjustment == null) try { MatColorAdjustment = new Material(Shader.Find("Hidden/FMETPColorAdjustment")); } catch (Exception e) { Debug.LogWarning(e); }
            }
        }

        private void Reset() { AssignMaterials(true); }

        // Use this for initialization
        private void Start()
        {
            Application.runInBackground = true;
            AssignMaterials();
        }

        private bool ReadyToGetFrame = true;

        //[Header("Pair Encoder & Decoder")]
        [Range(1000, UInt16.MaxValue)] public UInt16 label = 1001;
        private UInt16 dataID = 0;
        //UInt16 maxID = 1024;
        private int dataLength = 0;
        private int receivedLength = 0;

        private byte[] dataByte;
        public bool GZipMode = false;

        public void Action_ProcessImageData(byte[] _byteData)
        {
            if (!enabled) return;
            if (_byteData.Length <= 14) return;

            UInt16 _label = BitConverter.ToUInt16(_byteData, 0);
            if (_label != label) return;
            UInt16 _dataID = BitConverter.ToUInt16(_byteData, 2);

            if (_dataID != dataID) receivedLength = 0;
            dataID = _dataID;
            dataLength = BitConverter.ToInt32(_byteData, 4);
            int _offset = BitConverter.ToInt32(_byteData, 8);

            GZipMode = _byteData[12] == 1;
            ColorReductionLevel = (int)_byteData[13];

            //check if the texture is Desktop frame?
            isDesktopFrame = _byteData[14] != 0 ? true : false;
            int metaByteLength = isDesktopFrame ? 24 : 15;

            if (receivedLength == 0) dataByte = new byte[dataLength];
            int chunkLength = _byteData.Length - metaByteLength;
            if (_offset + chunkLength <= dataByte.Length)
            {
                receivedLength += chunkLength;
                Buffer.BlockCopy(_byteData, metaByteLength, dataByte, _offset, chunkLength);
            }

            if (ReadyToGetFrame)
            {
                if (receivedLength == dataLength)
                {
                    if (DecoderDelay_old != DecoderDelay) StopAll();

                    if (this.isActiveAndEnabled)
                    {
                        if (isDesktopFrame)
                        {
                            fmDesktopMonitorScaling = (float)_byteData[14];
                            fmDesktopSystemOS = (FMDesktopSystemOS)((int)_byteData[15]);
                            fmDesktopFrameRect.x = (float)BitConverter.ToInt16(_byteData, 16);
                            fmDesktopFrameRect.y = (float)BitConverter.ToInt16(_byteData, 18);
                            fmDesktopFrameRect.width = (float)BitConverter.ToInt16(_byteData, 20);
                            fmDesktopFrameRect.height = (float)BitConverter.ToInt16(_byteData, 22);
                        }
                        StartCoroutine(ProcessImageData(dataByte));
                    }
                }
            }
        }

        private TextureFormat GetTextureFormat() { return (Mono && FastMode) ? TextureFormat.R8 : TextureFormat.RGB24; }
        private IEnumerator ProcessImageData(byte[] inputByteData)
        {
            if (DecoderDelay > 0) yield return new WaitForSeconds(DecoderDelay);
            ReadyToGetFrame = false;

            if (ReceivedTexture2D == null) { ReceivedTexture2D = new Texture2D(1, 1, GetTextureFormat(), false); }
            else
            {
                if (ReceivedTexture2D.format != GetTextureFormat())
                {
                    Destroy(ReceivedTexture2D);
                    ReceivedTexture2D = new Texture2D(1, 1, GetTextureFormat(), false);
                }
            }

#if UNITY_IOS && !UNITY_EDITOR
            FastMode = true;
#endif

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_WIN || UNITY_IOS || UNITY_ANDROID || WINDOWS_UWP || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        //supported fast mode
#else
            //not supported fast mode
            FastMode = false;
#endif

            if (FastMode)
            {
                //try AsyncMode, on supported platform
                if (AsyncMode && Loom.numThreads < Loom.maxThreads)
                {
                    //has spare thread
                    byte[] RawTextureData = new byte[1];
                    int _width = 0, _height = 0;

                    //need to clone a buffer for multi-threading
                    byte[] _bufferByte = new byte[inputByteData.Length];
                    Buffer.BlockCopy(inputByteData, 0, _bufferByte, 0, inputByteData.Length);

                    bool AsyncDecoding = true;
                    Loom.RunAsync(() =>
                    {
                        if (GZipMode) try { inputByteData = _bufferByte.FMUnzipBytes(); } catch { }
                        try { inputByteData.FMJPGToRawTextureData(ref RawTextureData, ref _width, ref _height, Mono ? TextureFormat.R8 : TextureFormat.RGB24); } catch { }
                        AsyncDecoding = false;
                    });
                    while (AsyncDecoding) yield return null;


                    if (RawTextureData.Length <= 8)
                    {
                        ReadyToGetFrame = true;
                        yield break;
                    }

                    try
                    {
                        //check resolution
                        ReceivedTexture2D.FMMatchResolution(ref ReceivedTexture2D, _width, _height);
                        ReceivedTexture2D.LoadRawTextureData(RawTextureData);
                        ReceivedTexture2D.Apply();
                    }
                    catch
                    {
                        Destroy(ReceivedTexture2D);
                        GC.Collect();

                        ReadyToGetFrame = true;
                        yield break;
                    }
                }
                else
                {
                    //no spare thread, run in main thread
                    if (GZipMode) yield return FMCoreTools_V3.RunCOR<byte[]>(FMCoreTools_V3.FMUnzippedByteCOR(inputByteData), (output) => inputByteData = output);
                    try { ReceivedTexture2D.FMLoadJPG(ref ReceivedTexture2D, inputByteData); }
                    catch
                    {
                        Destroy(ReceivedTexture2D);
                        GC.Collect();

                        ReadyToGetFrame = true;
                        yield break;
                    }
                }
            }
            else
            {
                if (GZipMode) yield return FMCoreTools_V3.RunCOR<byte[]>(FMCoreTools_V3.FMUnzippedByteCOR(inputByteData), (output) => inputByteData = output);
                try { ReceivedTexture2D.LoadImage(inputByteData); }
                catch
                {
                    Destroy(ReceivedTexture2D);
                    GC.Collect();

                    ReadyToGetFrame = true;
                    yield break;
                }
            }

            if (ReceivedTexture2D.width <= 8)
            {
                //throw new Exception("texture is smaller than 8 x 8, wrong data");
                Debug.LogError("texture is smaller than 8 x 8, wrong data");
                ReadyToGetFrame = true;
                yield break;
            }

            if (ReceivedTexture2D.filterMode != DecodedFilterMode) ReceivedTexture2D.filterMode = DecodedFilterMode;
            if (ReceivedTexture2D.wrapMode != DecodedWrapMode) ReceivedTexture2D.wrapMode = DecodedWrapMode;

            if (ColorReductionLevel > 0 || DeNoise > 0 || Sharpen > 0)
            {
                //check is Mono
                if (ReceivedRenderTexture != null)
                {
                    if (ReceivedRenderTexture.format != (Mono ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32))
                    {
                        Destroy(ReceivedRenderTexture);
                        ReceivedRenderTexture = null;
                    }
                }
                if (ReceivedRenderTexture == null) ReceivedRenderTexture = new RenderTexture(ReceivedTexture2D.width, ReceivedTexture2D.height, 0, Mono ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32);
                if (ReceivedRenderTexture.filterMode != DecodedFilterMode) ReceivedRenderTexture.filterMode = DecodedFilterMode;
                if (ReceivedRenderTexture.wrapMode != DecodedWrapMode) ReceivedRenderTexture.wrapMode = DecodedWrapMode;

                float brightness = Mathf.Pow(2, ColorReductionLevel);
                MatColorAdjustment.SetFloat("_Brightness", brightness);
                MatColorAdjustment.SetFloat("_DeNoise", DeNoise);
                MatColorAdjustment.SetFloat("_Sharpen", Sharpen);
                Graphics.Blit(ReceivedTexture2D, ReceivedRenderTexture, MatColorAdjustment);
            }

            switch (PreviewType)
            {
                case GameViewPreviewType.None: break;
                case GameViewPreviewType.RawImage: PreviewRawImage.texture = ReceivedTexture; break;
                case GameViewPreviewType.MeshRenderer: PreviewMeshRenderer.material.mainTexture = ReceivedTexture; break;
            }
            OnReceivedTextureEvent.Invoke(ReceivedTexture);
            if (isDesktopFrame) OnReceivedDesktopFrameRectEvent.Invoke(fmDesktopFrameRect);


            ReadyToGetFrame = true;
            yield return null;
        }

        private void StopAll()
        {
            StopAllCoroutines();
            ReadyToGetFrame = true;
            DecoderDelay_old = DecoderDelay;
        }
        private void OnDisable() { StopAll(); }

        //Motion JPEG: frame buffer
        private static int frameBufferSize = 650000;//300000
        private byte[] frameBuffer = new byte[frameBufferSize];
        private const byte picMarker = 0xFF;
        private const byte picStart = 0xD8;
        private const byte picEnd = 0xD9;

        private int frameIdx = 0;
        private bool inPicture = false;
        private byte previous = (byte)0;
        private byte current = (byte)0;

        private int idx = 0;
        private int streamLength = 0;

        public void Action_ProcessMJPEGData(byte[] _byteData) { parseStreamBuffer(_byteData); }
        private void parseStreamBuffer(byte[] streamBuffer)
        {
            idx = 0;
            streamLength = streamBuffer.Length;

            while (idx < streamLength)
            {
                if (inPicture) { parsePicture(streamBuffer); }
                else { searchPicture(streamBuffer); }
            }
        }

        //look for a jpeg frame(begin with FF D8)
        private void searchPicture(byte[] streamBuffer)
        {
            do
            {
                previous = current;
                current = streamBuffer[idx++];

                // JPEG picture start ?
                if (previous == picMarker && current == picStart)
                {
                    frameIdx = 2;
                    frameBuffer[0] = picMarker;
                    frameBuffer[1] = picStart;
                    inPicture = true;
                    return;
                }
            } while (idx < streamLength);
        }

        //fill the frame buffer, until FFD9 is reach.
        private void parsePicture(byte[] streamBuffer)
        {
            do
            {
                previous = current;
                current = streamBuffer[idx++];

                if(frameIdx >= frameBufferSize)
                {
                    byte[] _frameBuffer = new byte[frameBufferSize];
                    Buffer.BlockCopy(frameBuffer, 0, _frameBuffer, 0, frameBuffer.Length);

                    //increase buffer, try double it
                    frameBufferSize = frameBufferSize <= (int.MaxValue / 2) ? frameBufferSize * 2 : int.MaxValue;
                    frameBuffer = new byte[frameBufferSize];
                    Buffer.BlockCopy(_frameBuffer, 0, frameBuffer, 0, _frameBuffer.Length);
                }

                frameBuffer[frameIdx++] = current;

                // JPEG picture end ?
                if (previous == picMarker && current == picEnd)
                {
                    // Using a memorystream this way prevent arrays copy and allocations
                    using (MemoryStream s = new MemoryStream(frameBuffer, 0, frameIdx))
                    {
                        if (ReadyToGetFrame)
                        {
                            if (DecoderDelay_old != DecoderDelay) StopAll();
                            StartCoroutine(ProcessImageData(s.ToArray()));
                        }
                    }

                    inPicture = false;
                    return;
                }
            } while (idx < streamLength);
        }
    }
}