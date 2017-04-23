using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Media.Render;
using WinRTXamlToolkit.Async;

namespace Uncord.Models
{
    // audio graph sample
    // https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/AudioCreation/cs/AudioCreation/Scenario3_FrameInputNode.xaml.cs



    public class AudioPlaybackManager : IDisposable
    {
        public AudioGraph AudioGraph { get; private set; }

        public AsyncLock InitializeLock { get; } = new AsyncLock();

        public AudioDeviceOutputNode OutputNode { get; private set; }


        public AudioPlaybackManager()
        {
           
        }


        public void Dispose()
        {
            AudioGraph.Dispose();
        }

        public async Task Initialize()
        {
            using (var release = await InitializeLock.LockAsync())
            {
                // Create an AudioGraph with default settings
                AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Speech);
                
                CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

                if (result.Status != AudioGraphCreationStatus.Success)
                {
                    throw new Exception();
                }

                AudioGraph = result.Graph;

                var deviceOutputNodeCreateResult = await AudioGraph.CreateDeviceOutputNodeAsync();
                if (deviceOutputNodeCreateResult.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    throw new Exception();
                }
                var outputNode = deviceOutputNodeCreateResult.DeviceOutputNode;
                OutputNode = outputNode;
            }
        }

        private void AudioGraph_QuantumProcessed(AudioGraph sender, object args)
        {
//            System.Diagnostics.Debug.WriteLine(args);
        }

        public async Task<bool> CheckAvailable()
        {
            using (var release = await InitializeLock.LockAsync())
            {
                return AudioGraph != null;
            }
        }    
        

        
    }
}
