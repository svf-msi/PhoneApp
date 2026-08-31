using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Standard.Licensing;

namespace LicenseManager.Library
{
    public class CustomerLicense
    {
        #region Static part

        static int Counter { get; set; }
        public static ObservableCollection<Customer> Customers { get; set; }

        //public static CustomerLicense New(Customer customer)
        //{
        //    return new CustomerLicense
        //    {
        //        LicenseId = ++Counter,
        //        CustomerId = customer.Id,
        //    };
        //}

        public static CustomerLicense From(string licenseString)
        {
            try
            {
                if (string.IsNullOrEmpty(licenseString)) return null;

                var license = License.Load(licenseString);

                if (license == null) return null;

                var customerLicense = new CustomerLicense();

                customerLicense.Type = license.Type == LicenseType.Trial ? CustomerLicenseType.Trial : CustomerLicenseType.Full;
                customerLicense.LicenseString = licenseString;

                var features = license.ProductFeatures;
                var attributes = license.AdditionalAttributes;

                if (features.Contains("Module"))
                {
                    if (Enum.TryParse<ModuleType>(features.Get("Module"), out var module))
                        customerLicense.Module = module;
                }

                if (features.Contains("Package"))
                {
                    if (Enum.TryParse<PackageType>(features.Get("Package"), out var package))
                        customerLicense.Package = package;
                }

                if (features.Contains("Version"))
                {
                    customerLicense.Version = features.Get("Version");
                }

                if (attributes.Contains("Host"))
                {
                    customerLicense.HostId = attributes.Get("Host");
                }

                if (attributes.Contains("StartDate"))
                {
                    customerLicense.StartDate = attributes.Get("StartDate");
                }

                if (attributes.Contains("EndDate"))
                {
                    customerLicense.EndDate = attributes.Get("EndDate");
                }

                if (attributes.Contains("LicenseId"))
                {
                    if (int.TryParse(attributes.Get("LicenseId"), out var lic))
                        customerLicense.LicenseId = lic;
                }

                if (attributes.Contains("CustomerId"))
                {
                    if (int.TryParse(attributes.Get("CustomerId"), out var cus))
                        customerLicense.CustomerId = cus;
                }

                return customerLicense;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in customer license: {e.Message}");
                return null;
            }
        }

        public static void UpdateCount(IEnumerable<CustomerLicense> licenses)
        {
            if (licenses == null || licenses.Count() == 0) Counter = 0;
            else Counter = licenses.Select(license => license?.LicenseId ?? 0).Max();
        }

        #endregion

        public int LicenseId { get; set; }
        public int CustomerId { get; set; }
        public string Tag { get; set; }
        bool valid;
        public bool Valid { get => valid; set { valid = value; } }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public ModuleType Module { get; set; } = ModuleType.VibVue;
        public PackageType Package { get; set; } = PackageType.Lite;
        public CustomerLicenseType Type { get; set; } = CustomerLicenseType.Full;
        public string Version { get; set; } = "1.0";
        public string HostId { get; set; } = "";
        public string LicenseString { get; set; }
        public string OrderNumber { get; set; } = "";
        public string OtherInfo { get; set; } = "Empty license";
        [JsonIgnore]
        public bool LicenseGenerated => LicenseString != null;
        [JsonIgnore]
        public License License { get => LicenseGenerated ? License.Load(LicenseString) : null; set { LicenseString = value?.ToString(); } }
        //[JsonIgnore]
        //public Customer Customer => Customers?.FirstOrDefault(customer => customer.Id == CustomerId);
        //[JsonIgnore]
        //public string CustomerName => Customer?.Name;
        //[JsonIgnore]
        //public string CustomerCompany => Customer?.Company;

        public void Generate(string privateKey, string password)
        {
            var parameters = new LicenseParameters
            {
                Type = Type == CustomerLicenseType.Trial ? LicenseType.Trial : LicenseType.Standard,
                Module = Module.ToString(),
                Package = Package.ToString(),
                Version = Version,
                Host = HostId,
                LicenseId = LicenseId,
                CustomerId = CustomerId,
                //Customer = CustomerName,
                //Email = Customer?.Email
            };

            if (!DateTime.TryParse(StartDate, out var start))
            {
                start = DateTime.Now;
                StartDate = start.ToShortDateString();
            }
            parameters.StartDate = StartDate;

            if (!DateTime.TryParse(EndDate, out var _))
            {
                EndDate = "";
            }
            parameters.EndDate = EndDate;

            if (Type == CustomerLicenseType.Trial)
            {
                if (!DateTime.TryParse(EndDate, out var end))
                {
                    var trialPeriod = 30;
                    end = start.AddDays(trialPeriod);
                    EndDate = end.ToShortDateString();
                }

                parameters.Expiration = (int)Math.Max(0, Math.Ceiling((end - DateTime.Now).TotalDays));
            }

            License = ApplicationLicense.CreateLicense(privateKey, password, parameters);

            OtherInfo = $"License generated on {DateTime.Now}";

            Valid = true;

            //NotifyPropertyChanged(null);
        }

        public void Export(string filename)
        {
            if (!string.IsNullOrEmpty(filename))
                ApplicationLicense.Save(License, filename);
        }

        public bool IsValid(string publicKey)
        {
            return ApplicationLicense.IsValid(License, publicKey);
        }

        public bool Verify(string publicKey, out string status)
        {
            status = "Valid license";
            if (publicKey == null)
            {
                status = "Missing public key";
                return false;
            }

            var license = License;
            if (license == null)
            {
                status = "Missing license content";
                return false;
            }

            var licenseType = license.Type == LicenseType.Trial ? CustomerLicenseType.Trial : CustomerLicenseType.Full;
            if (licenseType != Type)
            {
                status = $"Invalid license type: {licenseType}";
                return false;
            }

            if (!ApplicationLicense.IsValid(license, publicKey))
            {
                status = "Invalid license signature";
                return false;
            }

            var parameters = ApplicationLicense.GetLicenseAttributes(license);

            if (Module.ToString() != parameters.Module)
            {
                status = $"Module mismatch: license module = {parameters.Module}";
                return false;
            }

            if (Package.ToString() != parameters.Package)
            {
                status = $"Package mismatch: license package = {parameters.Package}";
                return false;
            }

            if (Version != parameters.Version)
            {
                status = $"Version mismatch: version module = {parameters.Version}";
                return false;
            }

            if (HostId != parameters.Host)
            {
                status = $"Host ID mismatch: license host = {parameters.Host}";
                return false;
            }

            return true;
        }
    }
}
