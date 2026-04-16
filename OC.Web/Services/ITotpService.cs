namespace OC.Web.Services
{
    public interface ITotpService
    {
        string GenerateSecretBase32();
        string ProtectSecret(string base32Secret);
        string UnprotectSecret(string protectedSecret);
        bool VerifyCode(string base32Secret, string code);
        string GetManualEntryKey(string base32Secret);
    }
}
