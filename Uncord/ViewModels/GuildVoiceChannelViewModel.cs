using Discord.Audio;
using Discord.WebSocket;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Microsoft.Practices.Unity;
using Uncord.Models;
using Windows.Media.Audio;
using Windows.Media;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WinRTXamlToolkit.Async;

namespace Uncord.ViewModels
{
    // We are initializing a COM interface for use within the namespace
    // This interface allows access to memory at the byte level which we need to populate audio data that is generated
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]

    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public class GuildVoiceChannelViewModel : BindableBase
    {
        public static bool IsEnableAudioCapture { get; set; } = false;

        public SocketVoiceChannel VoiceChannel { get; private set; }

        IAudioClient _AudioClient;

        public MediaCapture MediaCapture { get; private set; }
        AudioPlaybackManager AudioManager;

        AudioOutStream AudioOutStream;

        AudioInStream AudioInStream;

        AudioFrameInputNode frameInputNode;

        public string Name { get; private set; }


        bool _IsInitialized = false;
        AsyncLock _InitializeLock = new AsyncLock();

        

        public GuildVoiceChannelViewModel(SocketVoiceChannel voiceChannel)
        {
            AudioManager = App.Current.Container.Resolve<AudioPlaybackManager>();
            VoiceChannel = voiceChannel;

            Name = VoiceChannel.Name;
        }


        public async Task Enter()
        {
            using (var releaser = await _InitializeLock.LockAsync())
            {
                if (!_IsInitialized)
                {
                    if (IsEnableAudioCapture)
                    {
                        await InitializeAudioCapture();
                    }
                    _IsInitialized = true;
                }
            }

            // ボイスチャンネルへの接続を開始
            // 音声の送信はConnectedイベント後
            // 受信はStreamCreatedイベント後に行われます
            await VoiceChannel.ConnectAsync((client) => 
            {
                _AudioClient = client;
                client.Connected += VoiceChannelConnected;
                client.Disconnected += VoiceChannelDisconnected;
                client.LatencyUpdated += VoiceChannelLatencyUpdated;
                client.SpeakingUpdated += VoiceChannelSpeakingUpdated;
                client.StreamCreated += VoiceChannelAudioStreamCreated;
                client.StreamDestroyed += VoiceChannelAudioStreamDestroyed;
            });
            
        }

        public async Task Leave()
        {
            _AudioClient.Dispose();

            await Task.Delay(0);
        }

        
        

        

        private async Task VoiceChannelConnected()
        {
            if (IsEnableAudioCapture)
            {
                await StartAudioCapture();
            }
        }

        private async Task VoiceChannelDisconnected(Exception arg)
        {
            await StopAudioCapture();
        }

        



        private async Task VoiceChannelAudioStreamCreated(ulong arg1, AudioInStream stream)
        {
            AudioInStream = stream;
            
            // 音声出力用のオーディオグラフ入力ノードを作成
            frameInputNode = AudioManager.AudioGraph.CreateFrameInputNode(
                AudioEncodingProperties.CreatePcm(
                    OpusConvertConstants.SamplingRate,
                    1,
                    OpusConvertConstants.SampleBits
                    ));

            // デフォルトの出力ノードに接続
            frameInputNode.AddOutgoingConnection(AudioManager.OutputNode);

            // 念のため止める
            frameInputNode.Stop();
            AudioManager.AudioGraph.ResetAllNodes();

            // オーディオグラフ入力のタイミングを制御するイベントを利用
            frameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;

            // 音声出力のオーディオグラフを開始
            frameInputNode.Start();
            AudioManager.AudioGraph.Start();

            await Task.Delay(0);
        }

        private async Task VoiceChannelAudioStreamDestroyed(ulong arg)
        {
            AudioManager.AudioGraph.Stop();
            
            frameInputNode.Stop();
            frameInputNode.Dispose();
            frameInputNode = null;

            // TODO: 意図しない切断の場合に、ボイスチャンネルへの再接続

            await Task.Delay(0);
        }


        private Task VoiceChannelSpeakingUpdated(ulong arg1, bool arg2)
        {
            return Task.CompletedTask;
        }

        private Task VoiceChannelLatencyUpdated(int arg1, int arg2)
        {
            return Task.CompletedTask;
        }



        #region AudioCapture

        private async Task InitializeAudioCapture()
        {
            this.MediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Audio
            };

            await this.MediaCapture.InitializeAsync(settings);
        }

        private async Task StartAudioCapture()
        {
            if (MediaCapture == null)
            {
                return;
            }
            if (_AudioClient == null)
            {
                return;
            }

            AudioOutStream = _AudioClient.CreateDirectPCMStream(AudioApplication.Voice);
            
            await this.MediaCapture.StartRecordToStreamAsync(
                MediaEncodingProfile.CreateWav(AudioEncodingQuality.Auto),
                AudioOutStream.AsRandomAccessStream());
        }

        

        private async Task StopAudioCapture()
        {
            if (MediaCapture != null)
            {
                await MediaCapture.StopRecordAsync();
            }

            if (AudioOutStream != null)
            {
                AudioOutStream.Dispose();
                AudioOutStream = null;
            }
        }


        #endregion




        #region Audio Output


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
    }
}
