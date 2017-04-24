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
using Windows.Media.MediaProperties;
using Microsoft.Practices.Unity;
using Uncord.Models;
using Windows.Media.Audio;
using WinRTXamlToolkit.Async;

namespace Uncord.ViewModels
{
    public class GuildVoiceChannelViewModel : BindableBase
    {
        public static bool IsEnableAudioCapture { get; set; } = true;
        public static bool IsEnableAudioInput { get; set; } = true;

        public SocketVoiceChannel VoiceChannel { get; private set; }

        IAudioClient _AudioClient;

        AudioPlaybackManager AudioManager;


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
            if (IsEnableAudioInput)
            {
                AudioManager.StartAudioOutput(stream);
            }

            await Task.Delay(0);
        }

        private async Task VoiceChannelAudioStreamDestroyed(ulong arg)
        {
            AudioManager.StopAudioOutput();
            
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


        private async Task StartAudioCapture()
        {
            if (_AudioClient == null)
            {
                return;
            }

            if (!IsEnableAudioCapture)
            {
                return;
            }

            AudioManager.StartAudioInput(_AudioClient);

            await Task.Delay(0);
        }

        

        private async Task StopAudioCapture()
        {
            AudioManager.StopAudioInput();

            await Task.Delay(0);
        }


        #endregion




        #region Audio Output


        

        #endregion
    }
}
