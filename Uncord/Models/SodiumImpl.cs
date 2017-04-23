using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncord.Models
{
    public sealed class SodiumImpl : Discord.Audio.IStreamCipher
    {
        public readonly static SodiumImpl Instance = new SodiumImpl();

        private SodiumImpl() { }

        public int Encrypt(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, byte[] nonce, byte[] secret)
        {
            var realInputBytes = input.Skip(inputOffset).Take(inputLength).ToArray();
            var result = Sodium.SecretBox.Create(realInputBytes, nonce, secret);
            result.CopyTo(output, outputOffset);
            return inputLength + 16;
        }

        public int Decrypt(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, byte[] nonce, byte[] secret)
        {
            var realInputBytes = input.Skip(inputOffset).Take(inputLength).ToArray();
            var decrypted = Sodium.SecretBox.Open(realInputBytes, nonce, secret);
            decrypted.CopyTo(output, outputOffset);
            return inputLength - 16;
        }

    }
}
