using Standard.Licensing;
using Standard.Licensing.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LicenseManager.Library
{
    public class ApplicationLicense
    {
        public static void CreateKeys(string password, out string privateKey, out string publicKey)
        {
            var keyGenerator = Standard.Licensing.Security.Cryptography.KeyGenerator.Create();
            var keyPair = keyGenerator.GenerateKeyPair();
            privateKey = keyPair.ToEncryptedPrivateKeyString(password);
            publicKey = keyPair.ToPublicKeyString();
            Console.WriteLine($"Pass phrase: {password} \nPrivate key: {privateKey} \nPublic key: {publicKey}");
        }

        public static void SaveKeys(string privateKey, string publicKey, string file)
        {
            File.WriteAllLines(file, new string[] { privateKey, publicKey });
        }

        public static bool ReadKeys(string file, out string privateKey, out string publicKey)
        {
            privateKey = "";
            publicKey = "";

            if (File.Exists(file))
            {
                var keys = File.ReadAllLines(file);
                if (keys.Length > 1)
                {
                    privateKey = keys[0];
                    publicKey = keys[1];
                    return true;
                }
            }

            return false;
        }

        static string Hash(string input)
        {
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (var b in hash) // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));

                return sb.ToString();
            }
        }

        public static License CreateLicense(string privateKey, string password, LicenseParameters attributes)
        {
            var license = License.New().WithUniqueIdentifier(Guid.NewGuid());

            if (attributes != null)
            {
                if (attributes.Type == LicenseType.Trial)
                {
                    license = license.As(LicenseType.Trial).ExpiresAt(DateTime.Now.AddDays(attributes.Expiration));
                }
                else
                {
                    license = license.As(LicenseType.Standard);
                }

                PopulateFeatures(license, attributes);
                PopulateAddons(license, attributes);

                if (!string.IsNullOrEmpty(attributes.Customer))
                {
                    license = license.LicensedTo(attributes.Customer, attributes.Email ?? "");
                }
            }

            return license.CreateAndSignWithPrivateKey(privateKey, password);
        }

        static void PopulateFeatures(ILicenseBuilder license, LicenseParameters attributes)
        {
            if (attributes != null)
            {
                var dict = new Dictionary<string, string>();
                Populate(dict, nameof(attributes.Module), attributes.Module);
                Populate(dict, nameof(attributes.Package), attributes.Package);
                Populate(dict, nameof(attributes.Version), attributes.Version);

                if (dict.Count > 0)
                {
                    license = license.WithProductFeatures(dict);
                }
            }
        }

        static void PopulateAddons(ILicenseBuilder license, LicenseParameters attributes)
        {
            if (attributes != null)
            {
                var dict = new Dictionary<string, string>();
                Populate(dict, nameof(attributes.Host), attributes.Host);
                Populate(dict, nameof(attributes.StartDate), attributes.StartDate);
                Populate(dict, nameof(attributes.EndDate), attributes.EndDate);
                Populate(dict, nameof(attributes.LicenseId), attributes.LicenseId.ToString());
                Populate(dict, nameof(attributes.CustomerId), attributes.CustomerId.ToString());

                if (dict.Count > 0)
                {
                    license = license.WithAdditionalAttributes(dict);
                }
            }
        }

        static void Populate(Dictionary<string, string> dict, string key, string value)
        {
            if (!string.IsNullOrEmpty(key)) dict[key] = value;
        }

        public static License Load(string file)
        {
            using (var fs = File.OpenRead(file))
            {
                return fs != null ? License.Load(fs) : null;
            }
        }

        public static void Save(License license, string file)
        {
            File.WriteAllText(file, license?.ToString(), Encoding.UTF8);
        }

        public static void Show(License license)
        {
            Console.WriteLine(license?.ToString());
        }

        public static List<IValidationFailure> Validate(License license, string publicKey)
        {
            if (license == null || string.IsNullOrEmpty(publicKey))
                return new List<IValidationFailure> { new GeneralValidationFailure { Message = "Empty license or key" } };
            try
            {
                return license.Validate()
                    .ExpirationDate()
                    .When(lic => lic.Type == LicenseType.Trial)
                    .And()
                    .Signature(publicKey)
                    .AssertValidLicense()
                    .ToList() ?? new List<IValidationFailure>();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in license validation: {e}");
                return new List<IValidationFailure> { new GeneralValidationFailure { Message = e.Message } };
            }
        }

        public static bool IsValid(License license, string publicKey)
        {
            return Validate(license, publicKey).Count == 0;
        }

        public static List<string> Errors(License license, string publicKey)
        {
            return Validate(license, publicKey).Select(failure => failure.GetType().Name + ": " + failure.Message + " - " + failure.HowToResolve).ToList();
        }

        public static LicenseParameters GetLicenseAttributes(License license)
        {
            var parameters = new LicenseParameters();

            try
            {
                var features = license.ProductFeatures;
                var attributes = license.AdditionalAttributes;

                var feature = features.Get(nameof(parameters.Module));
                if (feature != null)
                {
                    parameters.Module = feature;
                }

                feature = features.Get(nameof(parameters.Package));
                if (feature != null)
                {
                    parameters.Package = feature;
                }

                feature = features.Get(nameof(parameters.Version));
                if (feature != null)
                {
                    parameters.Version = feature;
                }

                feature = attributes.Get(nameof(parameters.Host));
                if (feature != null)
                {
                    parameters.Host = feature;
                }

                var customer = license.Customer;
                if (customer != null)
                {
                    parameters.Customer = customer.Name;
                    parameters.Email = customer.Email;
                }
            }

            catch (Exception e)
            {
                Console.WriteLine($"Error in reading license attributes: {e}");
            }

            return parameters;
        }
    }

}
