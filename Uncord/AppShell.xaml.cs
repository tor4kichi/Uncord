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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Uncord
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class AppShell : Page
    {
        public static readonly DependencyProperty IsMenuHideProperty =
           DependencyProperty.Register(
               nameof(IsMenuHide), // プロパティ名を指定
               typeof(bool), // プロパティの型を指定
               typeof(AppShell), // プロパティを所有する型を指定
               new PropertyMetadata(false)); // メタデータを指定。ここではデフォルト値を設定してる


        public bool IsMenuHide
        {
            get { return (bool)GetValue(IsMenuHideProperty); }
            set { SetValue(IsMenuHideProperty, value); }
        }

        public AppShell()
        {
            this.InitializeComponent();
        }


        public void SetContent(UIElement element)
        {
            PageContent.Content = element;
        }
    }
}
