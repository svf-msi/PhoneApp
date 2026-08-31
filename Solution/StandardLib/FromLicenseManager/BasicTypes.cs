using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenseManager.Library
{
    public class LicenseKeys
    {
        public string Password { get; protected set; }
        public string PrivateKey { get; protected set; }
        public string PublicKey { get; protected set; }

        public LicenseKeys(string password, string privateKey, string publicKey)
        {
            Password = password;
            PrivateKey = privateKey;
            PublicKey = publicKey;
        }
    }

    public enum ModuleType { None, VibVue, TrakVue, AlphaVue, NavVue, MicroVue }

    public enum PackageType { Lite, Pro, AnalysisLite, AnalysisPro, None }

    public enum CustomerLicenseType { Full, Trial }
}
