using System;

namespace WinGetYamlGenerator
{
    static class Extensions
    {
        internal static bool IsWebUrl(this Uri uri)
        {
            if (uri == null)
            {
                return false;
            }

            return uri.Scheme.ToLowerInvariant() == "http" || uri.Scheme.ToLowerInvariant() == "https";
        }

        internal static bool IsAcceptableDownalodUrl(this Uri uri)
        {
            if (uri == null)
            {
                return false;
            }

            var scheme = uri.Scheme.ToLowerInvariant();
            return (scheme == "http" || scheme == "https");
        }

        internal static Version ToCanonicalVersion(this Version version)
        {
            if (version.Revision >= 0)
            {
                return version;
            }

            if (version.Build >= 0)
            {
                return new Version(version.Major, version.Minor, version.Build, 0);
            }

            return new Version(version.Major, version.Minor, 0, 0);
        }

        internal static bool CompareCanonicalVersions(this Version version, Version other)
        {
            if (version == null && other == null)
            {
                return true;
            }

            if (version == null || other == null)
            {
                return false;
            }

            return version.ToCanonicalVersion() == other.ToCanonicalVersion();
        }

        internal static bool IsVersionZero(this Version version)
        {
            return version.CompareCanonicalVersions(new Version(0, 0));
        }
    }
}
