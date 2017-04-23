using Prism.Unity.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using System.Diagnostics;
using Discord.Audio;
using Windows.Media.Audio;
using Windows.Media.Render;

namespace Uncord
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : PrismUnityApplication
    {
        AppShell _AppShell;

        public bool IsHideMenu
        {
            get { return _AppShell.IsMenuHide; }
            set { _AppShell.IsMenuHide = value; }
        }

        /// <summary>
        /// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
        ///最初の行であるため、main() または WinMain() と論理的に等価です。
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine(e.ToString());
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }

        protected override void ConfigureContainer()
        {
            Container.RegisterInstance(new Models.DiscordContext());
            Container.RegisterInstance(new Models.AudioPlaybackManager());
            base.ConfigureContainer();
        }

        protected override Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
        {
            return Task.CompletedTask;
        }

        protected override UIElement CreateShell(Frame rootFrame)
        {
            var appShell = new AppShell();
            _AppShell = appShell;
            appShell.SetContent(rootFrame);


            // 自動ログインチェック
            var discordContext = Container.Resolve<Models.DiscordContext>();
            
            if (discordContext.IsValidateToken)
            {
                NavigationService.Navigate(PageTokens.LoggedInProcessPageToken, null);
            }
            else
            {
                NavigationService.Navigate(PageTokens.AccountLoginPageToken, null);
            }
            
            return appShell;
        }

        
        protected override async Task OnInitializeAsync(IActivatedEventArgs args)
        {
            var audioPlaybackManager = Container.Resolve<Models.AudioPlaybackManager>();
            await audioPlaybackManager.Initialize();

            Discord.Audio.Streams.OpusDecodeStream.OpusDecoderFactory = () => new Models.OpusDecoderImpl();
            Discord.Audio.Streams.OpusEncodeStream.OpusEncodeFactory = (bitrate, app, signal) => new Models.OpusEncoderImpl(bitrate, app, signal);


            Discord.Audio.Streams.SodiumDecryptStream.StreamCipher = Models.SodiumImpl.Instance;
            Discord.Audio.Streams.SodiumEncryptStream.StreamCipher = Models.SodiumImpl.Instance;

            // 自動ログインを行う
            var discordContext = Container.Resolve<Models.DiscordContext>();
            await discordContext.TryLoginWithRecordedCredential();
            await base.OnInitializeAsync(args);
        }

        

        /// <summary>
        /// 特定のページへの移動が失敗したときに呼び出されます
        /// </summary>
        /// <param name="sender">移動に失敗したフレーム</param>
        /// <param name="e">ナビゲーション エラーの詳細</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
        
    }
}
