using CredentialManagement;

namespace BRaVe_CardPrinting_DesktopApp.Auth
{
    public interface ITokenStore
    {
        string GetRefreshToken();

        void SaveRefreshToken(string refreshToken);

        string GetMasterKey();

        void SaveMasterKey(string masterKey);

        void Clear();
    }

    public class CredentialManagerTokenStore : ITokenStore
    {
        private const string RefreshTokenTarget ="BRaVeCardPrintingRefreshToken";

        private const string MasterKeyTarget ="BRaVeCardPrintingMasterKey";

        public string GetRefreshToken()
        {
            var cred = new Credential
            {
                Target = RefreshTokenTarget
            };

            if (!cred.Load())
                return "";

            return cred.Password; 
        }

        public void SaveRefreshToken(string refreshToken)
        {
            var cred = new Credential
            {
                Target = RefreshTokenTarget,
                Username = "BRaVeUser",
                Password = refreshToken, //ONLY refresh token
                PersistanceType = PersistanceType.LocalComputer
            };

            cred.Save();
        }

        public string GetMasterKey()
        {
            var cred = new Credential
            {
                Target = MasterKeyTarget
            };

            if (!cred.Load())
                return "";

            return cred.Password;
        }

        public void SaveMasterKey(string masterKey)
        {
            var cred = new Credential
            {
                Target = MasterKeyTarget,
                Username = "BRaVeMasterKey",
                Password = masterKey,
                PersistanceType = PersistanceType.LocalComputer
            };

            cred.Save();
        }

        public void Clear()
        {
            var refreshCred = new Credential
            {
                Target = RefreshTokenTarget
            };

            refreshCred.Delete();

            var masterKeyCred = new Credential
            {
                Target = MasterKeyTarget
            };

            masterKeyCred.Delete();
        }
    }
}