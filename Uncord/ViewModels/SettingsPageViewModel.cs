using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncord.Models;

namespace Uncord.ViewModels
{
    public class SettingsPageViewModel : UncordPageViewModelBase
    {
        public AudioPlaybackManager AudioPlaybackManager { get; }



        public static double MicMinVolume { get; } = AudioPlaybackManager.MicMinVolume * 1;
        public static double MicMaxVolume { get; } = AudioPlaybackManager.MicMaxVolume * 1;

        public static double SpeakerMinVolume { get; } = AudioPlaybackManager.SpeakerMinVolume * 1;
        public static double SpeakerMaxVolume { get; } = AudioPlaybackManager.SpeakerMaxVolume * 1;

        public ReactiveProperty<double> MicVolume { get; }
        public ReactiveProperty<double> SpeakerVolume { get; }

        public SettingsPageViewModel(AudioPlaybackManager audioManager)
        {
            AudioPlaybackManager = audioManager;

            MicVolume = audioManager.ToReactivePropertyAsSynchronized(
                x => x.MicVolume
                );
            SpeakerVolume = audioManager.ToReactivePropertyAsSynchronized(
                x => x.SpeakerVolume
                );
        }
    }
}
