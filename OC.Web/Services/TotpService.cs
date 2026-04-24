using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using System.Text;

namespace OC.Web.Services
{
    public class TotpService : ITotpService
    {
        private readonly IDataProtector _protector;

        public TotpService(IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector("PacienteTotpSecret.v1");
        }

        public string GenerateSecretBase32()
        {
            var bytes = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(bytes);
        }

        public string ProtectSecret(string base32Secret)
        {
            return _protector.Protect(base32Secret);
        }

        public string UnprotectSecret(string protectedSecret)
        {
            return _protector.Unprotect(protectedSecret);
        }

        public bool VerifyCode(string base32Secret, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var normalizedCode = new string(code.Where(char.IsDigit).ToArray());
            if (normalizedCode.Length != 6)
                return false;

            var secretBytes = Base32Encoding.ToBytes(base32Secret);
            var totp = new Totp(secretBytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);

            return totp.VerifyTotp(
                normalizedCode,
                out _,
                new VerificationWindow(previous: 1, future: 1));
        }

        public string GetManualEntryKey(string base32Secret)
        {
            var chunks = Enumerable.Range(0, (base32Secret.Length + 3) / 4)
                .Select(i =>
                {
                    var start = i * 4;
                    var len = Math.Min(4, base32Secret.Length - start);
                    return base32Secret.Substring(start, len);
                });
            return string.Join(" ", chunks);
        }
    }
}
