using Discord.Audio;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public class AudioPlaybackManager : BindableBase, IDisposable
    {
        // ユーザーのクライアント端末を中心に入出力方向を決定しています
        // Output = スピーカー、イヤホン
        // Input = マイク


        // 入出力先のデバイスごとにNodeが作成されるため、
        // ユーザーによるコントロールを受け付けられるようにします


        public AudioGraph AudioGraph { get; private set; }

        public AsyncLock InitializeLock { get; } = new AsyncLock();



        private AudioInputManager Input;

        public readonly static double DefaultMicSilentThreshold = 10.0;

        private InputDeviceState _InputDeviceState;
        public InputDeviceState InputDeviceState
        {
            get { return _InputDeviceState; }
            private set { SetProperty(ref _InputDeviceState, value); }
        }


        // マイク入力の無音設定
        // 発話中は20～40程度を示します
        // 無音判定として利用するのは 4~10 程度が良さそうです
        // 12あたりを越えると発話の閉じ部分が無音判定になってしまい
        // ブチッと音声が途切れたようになってしまうので注意してください
        /// <summary>
        /// マイク入力の無音設定 (4 ~ 10)
        /// </summary>
        public double MicSilentThreshold
        {
            get { return Input.SilentThreshold; }
            set { Input.SilentThreshold = value; }
        }

        private bool _IsMicMute;
        public bool IsMicMute
        {
            get { return _IsMicMute; }
            set
            {
                if (SetProperty(ref _IsMicMute, value))
                {
                    Input.IsMute = IsMicMute;
                }
            }
        }



        private AudioOutputManager Output;


        private bool _IsSpeakerMute;
        public bool IsSpeakerMute
        {
            get { return _IsSpeakerMute; }
            set
            {
                if (SetProperty(ref _IsSpeakerMute, value))
                {
                    Output.IsMute = IsSpeakerMute;
                }
            }
        }




        public AudioPlaybackManager()
        {
           
        }


        public void Dispose()
        {
            Input.Dispose();
            Output.Dispose();
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
                Input = await AudioInputManager.CreateAsync(AudioGraph);
                Input.InputDeviceStateChanged += Input_InputDeviceStateChanged;
                this.InputDeviceState = Input.InputDeviceState;

                // スピーカー出力を初期化
                Output = await AudioOutputManager.CreateAsync(AudioGraph);

            }
        }

        
        public void StartAudioOutput(Discord.Audio.AudioInStream audioInStream)
        {
            Output.StartAudioOutput(audioInStream);
        }
        public void StopAudioOutput()
        {
            Output.StopAudioOutput();
        }


        public Task StartAudioInput(IAudioClient audioClient)
        {
            return Input.StartAudioInput(audioClient);
        }

        public Task StopAudioInput()
        {
            return Input.StopAudioInput();
        }


        public async Task<bool> CheckAvailable()
        {
            using (var release = await InitializeLock.LockAsync())
            {
                return AudioGraph != null;
            }
        }


        public Task<bool> ResetMic(DeviceInformation micDevice = null)
        {
            return Input.ResetAudioInput(micDevice);
        }


        #region Event Handler

        private void Input_InputDeviceStateChanged(InputDeviceState obj)
        {
            InputDeviceState = obj;
        }

        #endregion

    }


    public class AudioInputManager : IDisposable
    {
        

        internal static async Task<AudioInputManager> CreateAsync(AudioGraph audioGraph, DeviceInformation microphoneDevice = null)
        {
            var audioInputManager = new AudioInputManager(audioGraph);
            await audioInputManager.ResetAudioInput();
            return audioInputManager;
        }

        private AudioGraph _AudioGraph;

        private InputDeviceState _InputDeviceState;
        public InputDeviceState InputDeviceState
        {
            get { return _InputDeviceState; }
            private set
            {
                if (_InputDeviceState != value)
                {
                    _InputDeviceState = value;
                    InputDeviceStateChanged?.Invoke(value);
                }
            }
        }


        

        public event Action<InputDeviceState> InputDeviceStateChanged;

        public double SilentThreshold { get; set; } = AudioPlaybackManager.DefaultMicSilentThreshold;

        private bool _IsMute;
        public bool IsMute
        {
            get { return _IsMute; }
            set
            {
                if (_IsMute != value)
                {
                    _IsMute = value;
                    if (_InputNode != null)
                    {
                        _InputNode.ConsumeInput = !_IsMute;
                    }
                }
            }
        }

        private AudioOutStream _AudioOutStream;


        private AudioDeviceInputNode _InputNode;

        private AudioFrameOutputNode _FrameOutputNode;

        private AsyncLock _OutputStreamLock = new AsyncLock();



        private AudioInputManager(AudioGraph audioGraph)
        {
            _AudioGraph = audioGraph;

            _MicCaptureDeviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.AudioCapture);
            _MicCaptureDeviceWatcher.Added += AudioWatcher_Added;
            _MicCaptureDeviceWatcher.Removed += AudioWatcher_Removed;
            _MicCaptureDeviceWatcher.Updated += AudioWatcher_Updated;
            _MicCaptureDeviceWatcher.EnumerationCompleted += AudioWatcher_EnumerationCompleted;

        }

        public void Dispose()
        {
            _InputNode.Dispose();
            _FrameOutputNode.Dispose();
            _MicCaptureDeviceWatcher.Stop();

            StopAudioInput().ConfigureAwait(false);
        }


        public static IAsyncOperation<DeviceInformationCollection> GetAllMicrophoneDevices()
        {
            return Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
        }



        public async Task<bool> ResetAudioInput(DeviceInformation microphoneDevice = null)
        {
            if (microphoneDevice == null)
            {
                var inputDevices = await GetAllMicrophoneDevices();
                if (inputDevices.Count == 0)
                {
                    InputDeviceState = InputDeviceState.MicrophoneNotDetected;
                    return false;
                }

                microphoneDevice = inputDevices[0];
            }

            var inputAudioEnocdingProperties = AudioEncodingProperties.CreatePcm(
                OpusConvertConstants.SamplingRate,
                1,
                16
                );

            var deviceInputNodeCreateResult = await _AudioGraph.CreateDeviceInputNodeAsync(
                Windows.Media.Capture.MediaCategory.GameChat,
                inputAudioEnocdingProperties,
                microphoneDevice
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

                return false;
            }

            _InputNode = deviceInputNodeCreateResult.DeviceInputNode;
            _FrameOutputNode = _AudioGraph.CreateFrameOutputNode(inputAudioEnocdingProperties);
            _InputNode.AddOutgoingConnection(_FrameOutputNode);

            InputDeviceState = InputDeviceState.Avairable;

            return true;
        }


        public async Task StartAudioInput(IAudioClient audioClient)
        {
            StartMicConnectWatcher();

            if (_AudioOutStream != null)
            {
                _AudioOutStream.Dispose();
                _AudioOutStream = null;
            }

            if (InputDeviceState != InputDeviceState.Avairable)
            {
                if (!await ResetAudioInput())
                {
                    return;
                }
            }

            _AudioOutStream = audioClient.CreatePCMStream(AudioApplication.Voice, 1920, 100);

            _FrameOutputNode.Stop();

            _AudioGraph.QuantumStarted += AudioGraph_QuantumStarted;

            _FrameOutputNode.Start();

            _AudioGraph.Start();
        }

        public async Task StopAudioInput()
        {
            StopMicConnectWatcher();

            if (_FrameOutputNode == null)
            {
                return;
            }

            if (_AudioOutStream == null)
            {
                return;
            }

            _FrameOutputNode.Stop();

            await _AudioOutStream.FlushAsync();

            using (var release = await _OutputStreamLock.LockAsync())
            {
                _AudioGraph.QuantumStarted -= AudioGraph_QuantumStarted;

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
                if (_IsMute)
                {
                    return;
                }

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
                    var audioBytes = GetAudioDataFromAudioFrame(
                        audioFrame,
                        out var soundLevel
                        );

                    if (soundLevel == double.NaN)
                    {
                        return;
                    }

                    var isSilent = soundLevel < SilentThreshold;
                    if (isSilent)
                    {
                        return;
                    }

                    try
                    {
                        await _AudioOutStream.WriteAsync(audioBytes, 0, audioBytes.Length);
                    }
                    catch (TaskCanceledException) { }
                    catch (OperationCanceledException) { }
                }
            }
        }


        


        private byte[] GetAudioDataFromAudioFrame(AudioFrame frame, out double outSoundLevel)
        {
            double sum = 0.0;
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

                        sum += Math.Abs(singleTmp);

                        byte[] chunkBytes = BitConverter.GetBytes(int16Tmp);
                        byteData[pos++] = chunkBytes[0];
                        byteData[pos++] = chunkBytes[1];

                        // Note: マイク入力を1チャンネルで取っている場合に、
                        // ステレオとして送るため1チャンネル分追加で
                        byteData[pos++] = chunkBytes[0];
                        byteData[pos++] = chunkBytes[1];
                    }

                    // 1サンプルあたり4byte使っているため4で割ってサンプル数を算出
                    var sampleCount = buffer.Length / 4;

                    // 1000掛けているのは扱いやすくするため
                    // outSoundLevelは無音時で5.0~8.0, 発話中で20.0以上程度を示す
                    outSoundLevel = (sum / sampleCount) * 1000; 

#if DEBUG
                    Debug.WriteLine($"{sum} | {buffer.Length} | {sum / sampleCount * 1000}");
#endif                    

                    return byteData;
                }
            }
        }


        DeviceWatcher _MicCaptureDeviceWatcher;
        private void StartMicConnectWatcher()
        {
            _MicCaptureDeviceWatcher.Start();
        }

        private void StopMicConnectWatcher()
        {
            _MicCaptureDeviceWatcher.Stop();
        }

        private void AudioWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
        }

        private void AudioWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            
        }

        private async void AudioWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            await ResetAudioInput();   
        }

        private async void AudioWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            await ResetAudioInput();
        }
    }

    public class AudioOutputManager : IDisposable
    {
        private AudioGraph _AudioGraph;

        public Discord.Audio.AudioInStream AudioInStream { get; private set; }

        private bool _IsMute;
        public bool IsMute
        {
            get { return _IsMute; }
            internal set
            {
                if (_IsMute != value)
                {
                    _IsMute = value;
                    if (_OutputNode != null)
                    {
                        _OutputNode.ConsumeInput = !_IsMute;
                    }
                }
            }
        }

        private AudioDeviceOutputNode _OutputNode;

        private AudioFrameInputNode _FrameInputNode;


        public static async Task<AudioOutputManager> CreateAsync(AudioGraph audioGraph)
        {
            var outputAudioManager = new AudioOutputManager(audioGraph);
            await outputAudioManager.InitializeAudioOutput();
            return outputAudioManager;
        }

        private AudioOutputManager(AudioGraph audioGraph)
        {
            _AudioGraph = audioGraph;
        }


        public void Dispose()
        {
            _OutputNode.Dispose();
            _FrameInputNode.Dispose();
        }


        private async Task InitializeAudioOutput()
        {
            var deviceOutputNodeCreateResult = await _AudioGraph.CreateDeviceOutputNodeAsync();
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
            // Note: Channels = 2 かつ BitRateがDisocrdに合わせて16bitじゃなきゃダメ
            #region DO NOT TOUCH

            _FrameInputNode = _AudioGraph.CreateFrameInputNode(
                AudioEncodingProperties.CreatePcm(
                    OpusConvertConstants.SamplingRate,
                    OpusConvertConstants.Channels,
                    16
                    ));

            #endregion

            // デフォルトの出力ノードに接続
            _FrameInputNode.AddOutgoingConnection(_OutputNode);


            _FrameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;

            _FrameInputNode.Start();

            _AudioGraph.Start();
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
                return;
                //throw new Exception("not connected to discord audio channel.");
            }

            if (AudioInStream.AvailableFrames == 0)
            {
                return;
            }

            uint numSamplesNeeded = (uint)args.RequiredSamples;

            if (numSamplesNeeded == 0)
            {
                return;
            }

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
    }

    public enum InputDeviceState
    {
        Avairable,
        MicrophoneNotDetected,
        AccessDenied,
        UnknowunError,
    }
    
}
