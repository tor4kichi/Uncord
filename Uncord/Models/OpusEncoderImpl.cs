using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncord.Models
{
    public sealed class OpusEncoderImpl : Discord.Audio.IOpusEncoder
    {
        public libopusUWP.OpusEncoderImpl Encoder { get; private set; }

        private byte[] _buffer;

        public Discord.Audio.AudioApplication AudioApplication { get; }
        public int BitRate { get; }

        public OpusEncoderImpl(int bitrate, Discord.Audio.OpusApplication opusApplication, Discord.Audio.OpusSignal opusSignal)
        {
            _buffer = new byte[OpusConvertConstants.FrameBytes];

            Encoder = new libopusUWP.OpusEncoderImpl(
                OpusConvertConstants.SamplingRate, 
                OpusConvertConstants.Channels, 
                (int)opusApplication
                );

            Encoder.EncoderCtl((int)Discord.Audio.OpusCtl.SetSignal, (int)opusSignal);
            Encoder.EncoderCtl((int)Discord.Audio.OpusCtl.SetPacketLossPercent, 30);
            Encoder.EncoderCtl((int)Discord.Audio.OpusCtl.SetInbandFEC, 1); //True
            Encoder.EncoderCtl((int)Discord.Audio.OpusCtl.SetBitrate, bitrate);
        }

        public void Dispose()
        {
            Encoder = null;
            
        }

        public int EncodeFrame(byte[] input, int inputOffset, byte[] output, int outputOffset)
        {
            if (outputOffset != 0)
            {
                var result = Encoder.Encode(
                    input.Skip(inputOffset).ToArray(),
                    OpusConvertConstants.FrameSamplesPerChannel,
                    _buffer,
                    output.Length - outputOffset
                    );

                _buffer.CopyTo(output, outputOffset);

                return result;
            }
            else
            {
                return Encoder.Encode(
                    input.Skip(inputOffset).ToArray(), 
                    OpusConvertConstants.FrameSamplesPerChannel, 
                    output, 
                    output.Length
                    );
            }
        }
    }
}
