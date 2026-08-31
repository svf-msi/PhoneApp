using Standard.Licensing;

namespace LicenseManager.Library
{
    public class LicenseParameters
    {
        // Primary
        public LicenseType Type { get; set; } = LicenseType.Standard;
        public int Expiration { get; set; }

        // License features
        public string Module { get; set; }
        public string Package { get; set; }
        public string Version { get; set; }

        // Additional attributes
        public string Host { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int LicenseId { get; set; }
        public int CustomerId { get; set; }

        // Customer info
        public string Customer { get; set; }
        public string Email { get; set; }
    }
}
