using System.Security.Cryptography;
using System.Text;

namespace ApiDiscount.Helpers
{
    public class CodeGenerator
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public string Next(int length)
        {
            if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));

            var buffer = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(buffer);

            var sb = new StringBuilder(length);
            foreach (var b in buffer)
            {
                sb.Append(Alphabet[b % Alphabet.Length]);
            }

            return sb.ToString();
        }
    }
}
