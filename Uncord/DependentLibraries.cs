using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncord.Models
{
    public static class DependentLibraries
    {

        public static AppDescription UncordAppDescription { get; } = new AppDescription()
        {
            Name = "Uncord",
            Url = "https://github.com/tor4kichi/Uncord"
        };

        public static List<AppDescription> Libraries { get; } = new List<AppDescription>()
        {
            new AppDescription()
            {
                Name = "Discord.Net",
                Url = "https://github.com/RogueException/Discord.Net",
            },
            new AppDescription()
            {
                Name = "libopus",
                Url = "http://opus-codec.org/",
            },
            new AppDescription()
            {
                Name = "libsodium-uwp",
                Url = "https://github.com/charlesportwoodii/libsodium-uwp",
            },
            new AppDescription()
            {
                Name = "ReactiveProperty",
                Url = "https://github.com/runceel/ReactiveProperty",
            },
            new AppDescription()
            {
                Name = "Prism",
                Url = "https://github.com/PrismLibrary/Prism",
            },
            new AppDescription()
            {
                Name = "MahApps.Metro.IconPacks",
                Url = "https://github.com/MahApps/MahApps.Metro.IconPacks",
            },
            new AppDescription()
            {
                Name = "Newtonsoft.Json",
                Url = "http://www.newtonsoft.com/json",
            },
            new AppDescription()
            {
                Name = "UWP Community Toolkit",
                Url = "https://github.com/Microsoft/UWPCommunityToolkit",
            },
            new AppDescription()
            {
                Name = "WinRT Xaml Toolkit",
                Url = "https://github.com/xyzzer/WinRTXamlToolkit",
            },
        };

    }

    public class AppDescription
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}
