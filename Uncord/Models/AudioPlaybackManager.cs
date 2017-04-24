﻿using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage.Streams;
using WinRTXamlToolkit.Async;

namespace Uncord.Models
{
    // audio graph sample
    // https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/AudioCreation/cs/AudioCreation/Scenario3_FrameInputNode.xaml.cs

    // We are initializing a COM interface for use within the namespace
    // This interface allows access to memory at the byte level which we need to populate audio data that is generated
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public class AudioPlaybackManager : IDisposable
    {
        // ユーザーのクライアント端末を中心に入出力方向を決定しています
        // Output = スピーカー、イヤホン
        // Input = マイク


        // 入出力先のデバイスごとにNodeが作成されるため、
        // ユーザーによるコントロールを受け付けられるようにします


        public AudioGraph AudioGraph { get; private set; }

        public AsyncLock InitializeLock { get; } = new AsyncLock();

        #region Output Audio

        
        private Discord.Audio.AudioInStream AudioInStream;

        private AudioDeviceOutputNode _OutputNode;

        private AudioFrameInputNode _FrameInputNode;





        #endregion


        #region Input Audio

        public InputDeviceState InputDeviceState { get; private set; }


        private AudioOutStream _AudioOutStream;


        private AudioDeviceInputNode _InputNode;

        private AudioFrameOutputNode _FrameOutputNode;

        private AsyncLock _OutputStreamLock = new AsyncLock();

        #endregion 

        public AudioPlaybackManager()
        {
           
        }


        public void Dispose()
        {
            AudioGraph.Dispose();
        }

        public async Task Initialize()
        {
            using (var release = await InitializeLock.LockAsync())
            {
                
                var pcmEncoding = AudioEncodingProperties.CreatePcm(48000, 1, 16);

                var result = await AudioGraph.CreateAsync(
                    new AudioGraphSettings(AudioRenderCategory.GameChat)
                    {
                        DesiredRenderDeviceAudioProcessing = AudioProcessing.Raw,
                        AudioRenderCategory = AudioRenderCategory.GameChat,
                        EncodingProperties = pcmEncoding
                    }
                );

                if (result.Status != AudioGraphCreationStatus.Success)
                {
                    throw new Exception();
                }

                AudioGraph = result.Graph;

                // マイク入力を初期化
                await InitializeAudioInput();

                // スピーカー出力を初期化
                await InitializeAudioOutput();

            }
        }

        
        #region Audio Output


        private async Task InitializeAudioOutput()
        {
            var deviceOutputNodeCreateResult = await AudioGraph.CreateDeviceOutputNodeAsync();
            if (deviceOutputNodeCreateResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception(deviceOutputNodeCreateResult.Status.ToString());
            }
            var outputNode = deviceOutputNodeCreateResult.DeviceOutputNode;
            _OutputNode = outputNode;
        }


        public void StartAudioOutput(Discord.Audio.AudioInStream audioInStream)
        {
            AudioInStream = audioInStream;

            // 音声出力用のオーディオグラフ入力ノードを作成
            _FrameInputNode = AudioGraph.CreateFrameInputNode(
                AudioEncodingProperties.CreatePcm(
                    OpusConvertConstants.SamplingRate,
                    1,
                    OpusConvertConstants.SampleBits
                    ));

            // デフォルトの出力ノードに接続
            _FrameInputNode.AddOutgoingConnection(_OutputNode);


            _FrameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;

            _FrameInputNode.Start();

            AudioGraph.Start();
        }


        public void StopAudioOutput()
        {
            AudioInStream = null;

            _FrameInputNode?.Stop();
            _FrameInputNode?.Dispose();
            _FrameInputNode = null;


        }

        /// <summary>
        /// 音声出力の
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void FrameInputNode_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
        {
            if (AudioInStream == null)
            {
                throw new Exception("not connected to discord audio channel.");
            }

            if (AudioInStream.AvailableFrames == 0)
            {
                return;
            }

            uint numSamplesNeeded = (uint)args.RequiredSamples;
            // audioDataのサイズはAudioInStream内のFrameが示すバッファサイズと同一サイズにしておくべきだけど
            var sampleNeededBytes = numSamplesNeeded * OpusConvertConstants.SampleBytes * OpusConvertConstants.Channels;

            // Note: staticで持たせるべき？
            var audioData = new byte[sampleNeededBytes];

            var result = await AudioInStream.ReadAsync(audioData, 0, (int)sampleNeededBytes);

            AudioFrame audioFrame = GenerateAudioData(audioData, (uint)result);
            sender.AddFrame(audioFrame);
        }



        unsafe AudioFrame GenerateAudioData(byte[] readedData, uint audioDataLength)
        {
            AudioFrame frame = new Windows.Media.AudioFrame((uint)audioDataLength);
            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                for (int i = 0; i < audioDataLength; i++)
                {
                    dataInBytes[i] = readedData[i];
                }
            }

            return frame;
        }

        #endregion


        #region Audio Input


        private async Task InitializeAudioInput()
        {
            var inputDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            if (inputDevices.Count == 0)
            {
                InputDeviceState = InputDeviceState.MicrophoneNotDetected;
                return;
            }

            var inputAudioEnocdingProperties = AudioEncodingProperties.CreatePcm(
                OpusConvertConstants.SamplingRate,
                2,
                OpusConvertConstants.SampleBits
                );

            var deviceInputNodeCreateResult = await AudioGraph.CreateDeviceInputNodeAsync(
                Windows.Media.Capture.MediaCategory.GameChat,
                inputAudioEnocdingProperties,
                inputDevices[0]
                );

            if (deviceInputNodeCreateResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                if (deviceInputNodeCreateResult.Status == AudioDeviceNodeCreationStatus.AccessDenied)
                {
                    InputDeviceState = InputDeviceState.AccessDenied;
                }
                else
                {
                    InputDeviceState = InputDeviceState.UnknowunError;
                }

                return;
            }

            _InputNode = deviceInputNodeCreateResult.DeviceInputNode;
            _FrameOutputNode = AudioGraph.CreateFrameOutputNode(inputAudioEnocdingProperties);
            _InputNode.AddOutgoingConnection(_FrameOutputNode);
        }


        public void StartAudioInput(IAudioClient audioClient)
        {
            if (_AudioOutStream != null)
            {
                _AudioOutStream.Dispose();
                _AudioOutStream = null;
            }

            _AudioOutStream = audioClient.CreatePCMStream(AudioApplication.Voice, 1920, 100);

            _FrameOutputNode.Stop();

            AudioGraph.QuantumStarted += AudioGraph_QuantumStarted;

            _FrameOutputNode.Start();

            AudioGraph.Start();
        }

        public async void StopAudioInput()
        {
            _FrameOutputNode.Stop();

            await _AudioOutStream.FlushAsync();

            using (var release = await _OutputStreamLock.LockAsync())
            {
                AudioGraph.QuantumStarted -= AudioGraph_QuantumStarted;

                if (_AudioOutStream != null)
                {
                    _AudioOutStream.Dispose();
                    _AudioOutStream = null;
                }
            }
        }

        private async void AudioGraph_QuantumStarted(AudioGraph sender, object args)
        {
            using (var release = await _OutputStreamLock.LockAsync())
            {
                if (_AudioOutStream == null)
                {
                    return;
                }

                if (_FrameOutputNode == null)
                {
                    return;
                }

                using (var audioFrame = _FrameOutputNode.GetFrame())
                {
                    var audioBytes = GetAudioDataFromAudioFrame(audioFrame);
                    
                    try
                    {
                        await _AudioOutStream.WriteAsync(audioBytes, 0, audioBytes.Length);
                    }
                    catch (TaskCanceledException) { }
                    catch (OperationCanceledException) { }
                }
            }
        }


        private byte[] GetAudioDataFromAudioFrame(AudioFrame frame)
        {
            using (var audioBuffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            {
                var buffer = Windows.Storage.Streams.Buffer.CreateCopyFromMemoryBuffer(audioBuffer);
                buffer.Length = audioBuffer.Length;

                using (var dataReader = DataReader.FromBuffer(buffer))
                {
                    dataReader.ByteOrder = ByteOrder.LittleEndian;

                    byte[] byteData = new byte[buffer.Length];
                    int pos = 0;

                    while (dataReader.UnconsumedBufferLength > 0)
                    {
                        /* Reading Float -> Int 16 */
                        
                        var singleTmp = dataReader.ReadSingle();
                        var int16Tmp = (Int16)(singleTmp * Int16.MaxValue);
                        byte[] chunkBytes = BitConverter.GetBytes(int16Tmp);
                        byteData[pos++] = chunkBytes[0];
                        byteData[pos++] = chunkBytes[1];
                    }

                    return byteData;
                }
            }
        }

        #endregion


        public async Task<bool> CheckAvailable()
        {
            using (var release = await InitializeLock.LockAsync())
            {
                return AudioGraph != null;
            }
        }    
    }

    public enum InputDeviceState
    {
        Avairable,
        MicrophoneNotDetected,
        AccessDenied,
        UnknowunError,
    }
    
}
