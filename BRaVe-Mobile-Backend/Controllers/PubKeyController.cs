using BRaVe_Mobile_Backend.Models;
using BRaVe_Mobile_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Mobile_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PubKeyController : ControllerBase
    {
        private AzureKeyVaultHelper _azureKeyVault;
        public PubKeyController(AzureKeyVaultHelper azureKeyVault) {
            _azureKeyVault = azureKeyVault;
        }

        [HttpGet]
        public  async Task<PublicKeyResponse> Get()
        {
            string pubkeyB64 = await _azureKeyVault.GetRsaPubKeyB64();

            return new PublicKeyResponse { 
                PublicKeyBase64 = pubkeyB64
            };

        }



    }
}
