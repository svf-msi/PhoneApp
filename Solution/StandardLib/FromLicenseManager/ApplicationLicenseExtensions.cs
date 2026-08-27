using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenseManager.Library
{
    public static class ApplicationLicenseExtensions
    {
        public static bool IsValidModule(this LicenseParameters parameters, string parameter)
        {
            return parameter == parameters.Module;
        }

        public static bool IsValidPackage(this LicenseParameters parameters, string parameter)
        {
            return parameter == parameters.Package;
        }

        public static bool IsValidVersion(this LicenseParameters parameters, int major, int minor)
        {
            var version = parameters.Version.Split('.');
            if (version.Length != 2) return false;

            if (!int.TryParse(version[0], out var majorVersion) || major > majorVersion)
            {
                return false;
            }

            if (!int.TryParse(version[1], out var minorVersion) || minor > minorVersion)
            {
                return false;
            }

            return true;
        }

        public static bool IsValidHost(this LicenseParameters parameters, string parameter)
        {
            return parameter == parameters.Host;
        }
    }
}
