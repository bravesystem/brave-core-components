using BRaVe_CardPrinting_DesktopApp.Api;
using BRaVe_CardPrinting_DesktopApp.Auth;
using BRaVe_CardPrinting_DesktopApp.Db;
using BRaVe_CardPrinting_DesktopApp.Helpers;
using BRaVe_CardPrinting_DesktopApp.Security;
using BRaVe_CardPrinting_DesktopApp.UI;
using Microsoft.Extensions.Configuration;
using SQLitePCL;
using System;
using System.Windows.Forms;

namespace BRaVe_CardPrinting_DesktopApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Batteries.Init();

            ApplicationConfiguration.Initialize();

            // ==========================================
            // INITIALIZE SQLITE TOKEN DB
            // ==========================================
            TokenDbInitializer.Initialize();

            // ==========================================
            // TOKEN STORE
            // ==========================================
            ITokenStore tokenStore =
                new CredentialManagerTokenStore();

            // ==========================================
            // LOAD OR CREATE MASTER AES KEY
            // ==========================================
            string masterKey =
                tokenStore.GetMasterKey();

            if (string.IsNullOrEmpty(masterKey))
            {
                using var aes =
                    System.Security.Cryptography.Aes.Create();

                aes.KeySize = 256;

                aes.GenerateKey();

                masterKey =
                    Convert.ToBase64String(aes.Key);

                tokenStore.SaveMasterKey(masterKey);
            }

            // ==========================================
            // CREATE CRYPTO SERVICES
            // ==========================================
            var keyBytes =
                Convert.FromBase64String(masterKey);

            var crypto =
                new TokenEncryptionService(keyBytes);

            var vault =
                new AccessTokenVault(crypto);

            // ==========================================
            // LOAD CONFIG
            // ==========================================
            var config = new ConfigurationBuilder()
               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
               .AddJsonFile("appsettings.json", optional: false)
               .Build();

            string baseUrl = config["Api:BaseUrl"];

            var api =
                new CardPrintingApiClient(baseUrl);

            // ==========================================
            // TRY LOCAL ACCESS TOKEN FIRST
            // ==========================================
            var storedToken =
                vault.LoadToken();

            if (storedToken != null)
            {
                // still valid
                if (storedToken.Value.ExpiryUtc > DateTime.UtcNow)
                {
                    SessionManager.AccessToken =
                        storedToken.Value.Token;

                    api.SetAccessToken(
                        storedToken.Value.Token);

                    Application.Run(new MainForm(api));

                    return;
                }
            }

            // ==========================================
            // FALLBACK TO REFRESH TOKEN
            // ==========================================
            var refreshToken =
                tokenStore.GetRefreshToken();

            if (!string.IsNullOrEmpty(refreshToken))
            {
                try
                {
                    var accessToken =
                        api.RefreshAsync(refreshToken).Result;

                    // save in memory
                    SessionManager.AccessToken =
                        accessToken;

                    // attach to API client
                    api.SetAccessToken(accessToken);

                    // save encrypted locally
                    vault.SaveToken(
                        accessToken,
                        DateTime.UtcNow.AddDays(30));

                    Application.Run(new MainForm(api));

                    return;
                }
                catch
                {
                    // clear invalid local token
                    vault.Clear();

                    // refresh failed -> reclaim
                    Application.Run(
                        new ClaimForm(tokenStore));
                }
            }
            else
            {
                Application.Run(
                    new ClaimForm(tokenStore));
            }
        }
    }
}