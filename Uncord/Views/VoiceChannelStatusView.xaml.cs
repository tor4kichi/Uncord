using Microsoft.Toolkit.Uwp.UI.Animations;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uncord.Views
{
    public sealed partial class VoiceChannelStatusView : UserControl
    {
        public VoiceChannelStatusView()
        {
            this.InitializeComponent();

            Loaded += VoiceChannelStatusView_Loaded;
        }

        private double? _ViewHeight;
        public double ViewHeight
        {
            get
            {
                if (_ViewHeight == null)
                {
                    _ViewHeight = (double)Resources["ViewHeight"];
                }

                return _ViewHeight.Value;
            }
        }

        IDisposable _PrevAnimDisposer;

        private void VoiceChannelStatusView_Loaded(object sender, RoutedEventArgs e)
        {
            var anim = VoiceChannelStatusViewLayout.Offset(offsetY: (float)ViewHeight)
                .SetDuration(0);
            anim.Start();
            anim.Dispose();
        }

        public void ShowVioceChannelStatusView()
        {
            _PrevAnimDisposer?.Dispose();

            VoiceChannelStatusViewLayout.Visibility = Visibility.Visible;
            var anim = VoiceChannelStatusViewLayout.Offset(0)
                .SetDuration(150);
            anim.Start();
            _PrevAnimDisposer = anim;
        }

        

        public void HideVioceChannelStatusView()
        {
            _PrevAnimDisposer?.Dispose();

            var anim = VoiceChannelStatusViewLayout.Offset(offsetY: (float)ViewHeight)
                .SetDuration(150);
            anim.Completed += HideVoiceChannelStatusViewCompleted;
            anim.Start();
            _PrevAnimDisposer = anim;
        }


        private void HideVoiceChannelStatusViewCompleted(object sender, AnimationSetCompletedEventArgs e)
        {
            VoiceChannelStatusViewLayout.Visibility = Visibility.Collapsed;
        }

    }
}
