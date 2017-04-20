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


namespace Uncord.ViewModels
{
    public class GuildVoiceChannelViewModel : BindableBase
    {
        public SocketVoiceChannel VoiceChannel { get; private set; }

        IAudioClient _AudioClient;

        public MediaCapture MediaCapture { get; private set; }


        public GuildVoiceChannelViewModel(SocketVoiceChannel voiceChannel)
        {
            VoiceChannel = voiceChannel;
        }


        public async Task Enter()
        {
            /*
            _AudioClient = await VoiceChannel.ConnectAsync();
            _AudioClient.Connected += AudioConnected;
            _AudioClient.Disconnected += AudioDisconnected;
            _AudioClient.LatencyUpdated += _AudioClient_LatencyUpdated;
            _AudioClient.SpeakingUpdated += _AudioClient_SpeakingUpdated;
            _AudioClient.StreamCreated += _AudioClient_StreamCreated;
            _AudioClient.StreamDestroyed += _AudioClient_StreamDestroyed;
            var outStream = _AudioClient.CreatePCMStream(AudioApplication.Voice, 120).AsRandomAccessStream();
            var mediaPlayer = new MediaPlayer();
            mediaPlayer.Source = MediaSource.CreateFromStream(outStream, "audio/wav");

            mediaPlayer.Play();

            */
            
            await Task.Delay(0);
        }

        private Task AudioDisconnected(Exception arg)
        {
            return Task.CompletedTask;
        }

        private Task AudioConnected()
        {
            return Task.CompletedTask;
        }


        private Task _AudioClient_StreamDestroyed(ulong arg)
        {
            throw new NotImplementedException();
        }

        private async Task _AudioClient_StreamCreated(ulong arg1, AudioInStream stream)
        {
            this.MediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Audio
            };

            await this.MediaCapture.InitializeAsync(settings);

            await this.MediaCapture.StartRecordToStreamAsync(
                MediaEncodingProfile.CreateWav(AudioEncodingQuality.Medium),
                stream.AsRandomAccessStream());

            
        }

        private Task _AudioClient_SpeakingUpdated(ulong arg1, bool arg2)
        {
            throw new NotImplementedException();
        }

        private Task _AudioClient_LatencyUpdated(int arg1, int arg2)
        {
            throw new NotImplementedException();
        }

        



        public async Task Leave()
        {
            await Task.Delay(0);
        }
    }
}
