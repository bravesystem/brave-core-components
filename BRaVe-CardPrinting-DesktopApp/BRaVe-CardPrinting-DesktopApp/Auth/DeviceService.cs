using Microsoft.Win32;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace BRaVe_CardPrinting_DesktopApp.Auth
{
    public class DeviceService
    {
        public string GetDeviceId()
        {
            const string keyPath =
                @"SOFTWARE\Microsoft\Cryptography";

            const string valueName = "MachineGuid";

            using var key =
                Registry.LocalMachine.OpenSubKey(keyPath);

            var machineGuid =
                key?.GetValue(valueName)?.ToString();

            if (string.IsNullOrWhiteSpace(machineGuid))
                throw new Exception(
                    "Unable to retrieve MachineGuid");

            using var sha = SHA256.Create();

            var hash = sha.ComputeHash(
                Encoding.UTF8.GetBytes(machineGuid));

            return Convert.ToHexString(hash);
        }
    }
}