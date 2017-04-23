using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncord.Models
{
    public sealed class OpusDecoderImpl : Discord.Audio.IOpusDecoder
    {

        public libopusUWP.OpusDecoderImpl Decoder { get; private set; }


        public OpusDecoderImpl()
        {
            Decoder = new libopusUWP.OpusDecoderImpl(OpusConvertConstants.SamplingRate, OpusConvertConstants.Channels);
        }

        public unsafe int DecodeFrame(byte[] input, int inputOffset, int inputCount, byte[] output, int outputOffset, bool decodeFEC)
        {
            int result = 0;

            var readInputBytes = input.Skip(inputOffset).ToArray();
            if (outputOffset != 0)
            {
                var _buffer = new byte[output.Length];

                result = Decoder.Decode(
                    readInputBytes, 
                    inputCount, 
                    _buffer, 
                    OpusConvertConstants.FrameSamplesPerChannel,
                    decodeFEC
                    );

                _buffer.CopyTo(output, outputOffset);
            }
            else
            {
                result =  Decoder.Decode(
                    readInputBytes,
                    inputCount,
                    output,
                    OpusConvertConstants.FrameSamplesPerChannel,
                    decodeFEC
                    );
            }

            return result;
        }

        public void Dispose()
        {
            Decoder = null;
        }
    }
}
