using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using WinRTXamlToolkit.Async;

namespace Uncord.Models
{
    // Discordへのログイン状態の保持
    // メール・パスワードの資格情報

    
    public class DiscordContext : BindableBase
    {
        public string DicordAccessToken { get; private set; }

        public bool IsValidateToken => !string.IsNullOrEmpty(DicordAccessToken);


        private AsyncLock _LoginLock = new AsyncLock();

        private string _LoginProcessStateResourceId;
        public string LoginProcessStateResourceId
        {
            get { return _LoginProcessStateResourceId; }
            set { SetProperty(ref _LoginProcessStateResourceId, value); }
        }

        private async Task StartLoginProcess()
        {
            // TODO: Discordへログインした後の初期化処理
        }

        private async Task ClearupLoginInfo()
        {
            // TODO: ログイン後の処理を中止
            // TODO: ログイン情報を全て破棄
        }

        public async Task WaitLoginAction()
        {
            using (var releaser = await _LoginLock.LockAsync())
            {
                // nothing to do.
            }
        }

        
        public async Task<bool> TryLoginWithRecordedCredential()
        {
            await LogOut();

            using (var releaser = await _LoginLock.LockAsync())
            {
                var vault = new Windows.Security.Credentials.PasswordVault();

                try
                {

                    if (!TryGetRecentLoginAccount(out var mailAndPassword))
                    {
                        return false;
                    }

                    var token = await Util.DiscordApiHelper.LogInAsync(mailAndPassword.Item1, mailAndPassword.Item2);
                    if (string.IsNullOrEmpty(token))
                    {
                        return false;
                    }

                    DicordAccessToken = token;
                }
                catch
                {
                    DicordAccessToken = null;
                    return false;
                }
            }

            if (IsValidateToken)
            {
                await StartLoginProcess().ConfigureAwait(false);
            }

            return true;
        }

        public async Task<bool> TryLogin(string mail, string password)
        {
            await LogOut();

            using (var releaser = await _LoginLock.LockAsync())
            {
                var token = await Util.DiscordApiHelper.LogInAsync(mail, password);
                if (token == null)
                {
                    return false;
                }

                DicordAccessToken = token;

                AddOrUpdateRecentLoginAccount(mail, password);
            }

            if (IsValidateToken)
            {
                await StartLoginProcess().ConfigureAwait(false);
            }

            return true;
        }

        public async Task LogOut()
        {
            bool nowLogout = false;
            using (var releaser = await _LoginLock.LockAsync())
            {
                if (IsValidateToken)
                {
                    if (!await Util.DiscordApiHelper.LogOutAsync(DicordAccessToken))
                    {
                        Debug.WriteLine("failed logout. token is " + DicordAccessToken);
                    }

                    DicordAccessToken = null;

                    nowLogout = true;
                }
            }

            if (nowLogout)
            {
                await ClearupLoginInfo().ConfigureAwait(false);
            }
        }



        public bool TryGetRecentLoginAccount(out Tuple<string, string> mailAndPassword)
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            var oldItems = vault.FindAllByResource(nameof(Uncord));
            if (oldItems.Count == 0)
            {
                mailAndPassword = null;
                return false;
            }
            var prevAccount = oldItems.First();
            var retrievedAccount = vault.Retrieve(prevAccount.Resource, prevAccount.UserName);

            if (string.IsNullOrWhiteSpace(retrievedAccount.UserName)
                || string.IsNullOrWhiteSpace(retrievedAccount.Password)
                )
            {
                mailAndPassword = null;
                return false;
            }

            mailAndPassword = new Tuple<string, string>(retrievedAccount.UserName, retrievedAccount.Password);

            return true;
        }

        private void AddOrUpdateRecentLoginAccount(string mail, string password)
        {
            try
            {
                var vault = new Windows.Security.Credentials.PasswordVault();
                try
                {
                    var oldItems = vault.FindAllByResource(nameof(Uncord));
                    foreach (var vaultItem in oldItems)
                    {
                        vault.Remove(vaultItem);
                    }
                    oldItems = null;
                }
                catch { }

                vault.Add(new Windows.Security.Credentials.PasswordCredential(
                    nameof(Uncord), mail, password));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
