using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System.Reactive.Disposables;
using Reactive.Bindings.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uncord.Views
{
    public sealed partial class MenuView : UserControl
    {
        public MenuView()
        {
            this.InitializeComponent();

            Loaded += MenuView_Loaded;
        }

        private void MenuView_Loaded(object sender, RoutedEventArgs e)
        {
            var anim = ChannelsLayout.Fade(duration: 0);
            anim.Start();
            anim.Completed += OnShowServers;
            anim.Dispose();
        }

        

        CompositeDisposable _AnimationDisposer;

        static readonly TimeSpan ToggleDuration = TimeSpan.FromSeconds(0.175);

        public void ToggleShowServersLayout()
        {
            _AnimationDisposer?.Dispose();
            _AnimationDisposer = new CompositeDisposable();

            var fade = ServersLayout
                .Fade(value: 1.0f)
                .Offset(offsetX: 0)
                .SetDelay(ToggleDuration.TotalMilliseconds * 0.5)
                .SetDurationForAll(ToggleDuration.TotalMilliseconds)
                .AddTo(_AnimationDisposer);
            var fade2 = ChannelsLayout
                .Fade(value: 0.0f)
                .Offset(offsetX: 30)
                .SetDurationForAll(ToggleDuration.TotalMilliseconds)
                .AddTo(_AnimationDisposer);

            ChannelsLayout.Visibility = Visibility.Visible;
            ServersLayout.Visibility = Visibility.Visible;

            fade.Completed += OnShowServers;

            fade.Start();
            fade2.Start();
        }

        public void ToggleShowServerChannelsLayout()
        {
            _AnimationDisposer?.Dispose();
            _AnimationDisposer = new CompositeDisposable();

            var fade = ServersLayout
                .Fade(value: 0.0f)
                .Offset(offsetX: -30)
                .SetDurationForAll(ToggleDuration.TotalMilliseconds)
                .AddTo(_AnimationDisposer);
            var fade2 = ChannelsLayout
                .Fade(value: 1.0f)
                .Offset(offsetX: 0)
                .SetDelay(ToggleDuration.TotalMilliseconds * 0.5)
                .SetDurationForAll(ToggleDuration.TotalMilliseconds)
                .AddTo(_AnimationDisposer);

            fade.Completed += OnShowServerChannels;

            ChannelsLayout.Visibility = Visibility.Visible;
            ServersLayout.Visibility = Visibility.Visible;

            fade.Start();
            fade2.Start();
        }

        private void OnShowServers(object sender, AnimationSetCompletedEventArgs e)
        {
            ChannelsLayout.Visibility = Visibility.Collapsed;
            ServersLayout.Visibility = Visibility.Visible;
        }

        private void OnShowServerChannels(object sender, AnimationSetCompletedEventArgs e)
        {
            ChannelsLayout.Visibility = Visibility.Visible;
            ServersLayout.Visibility = Visibility.Collapsed;
        }
    }
}
