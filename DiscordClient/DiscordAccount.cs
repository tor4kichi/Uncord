namespace DiscordClient
{
    public class DiscordAccount
    {
        public DiscordAccount(string mail , string password, string token)
        {
            Mail = mail;
            Password = password;
            Token = token;
        }
        public string Mail { get; private set; }
        public string Password { get; private set; }

        public string Token { get; private set; }
    }
}
