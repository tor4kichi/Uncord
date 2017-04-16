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
    public sealed partial class GuildTextChannelControl : UserControl
    {
        public static readonly DependencyProperty IsValidDataContextProperty =
           DependencyProperty.Register(
               nameof(IsValidDataContext), // プロパティ名を指定
               typeof(bool), // プロパティの型を指定
               typeof(GuildTextChannelControl), // プロパティを所有する型を指定
               new PropertyMetadata(false)); // メタデータを指定。ここではデフォルト値を設定してる


        public bool IsValidDataContext
        {
            get { return (bool)GetValue(IsValidDataContextProperty); }
            set { SetValue(IsValidDataContextProperty, value); }
        }


        public static readonly DependencyProperty HeaderMerginProperty =
           DependencyProperty.Register(
               nameof(HeaderMergin), // プロパティ名を指定
               typeof(Thickness), // プロパティの型を指定
               typeof(GuildTextChannelControl), // プロパティを所有する型を指定
               new PropertyMetadata(new Thickness(0,0,0,0))); // メタデータを指定。ここではデフォルト値を設定してる


        public Thickness HeaderMergin
        {
            get { return (Thickness)GetValue(HeaderMerginProperty); }
            set { SetValue(HeaderMerginProperty, value); }
        }

        public GuildTextChannelControl()
        {
            this.InitializeComponent();

            this.DataContextChanged += GuildTextChannelControl_DataContextChanged;

            CheckValidDataContext();
        }

        private void GuildTextChannelControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            CheckValidDataContext();
        }

        private void CheckValidDataContext()
        {
            IsValidDataContext = DataContext is ViewModels.GuildTextChannelViewModel;
        }
    }
}
