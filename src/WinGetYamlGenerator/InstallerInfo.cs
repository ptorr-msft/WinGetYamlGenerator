
using System;
using System.Collections.Generic;

namespace WinGetYamlGenerator
{
    public enum ArchitectureKind
    {
        x86,
        x64,
        ARM,
        ARM64
    }

    public enum InstallerKind
    {
        EXE,
        MSI,
        MSIX,
        APPX,
        INNO,
        WIX,
        NULLSOFT,
    }

    public class InstallerInfo : PropertyChangedBase
    {
        ArchitectureKind arch;
        public ArchitectureKind Architecture
        {
            get => arch;
            set => Set(value, ref arch);
        }

        InstallerKind kind;
        public InstallerKind InstallerKind
        {
            get => kind;
            set => Set(value, ref kind);
        }

        Uri uri;
        public Uri Uri
        {
            get => uri;
            set => Set(value, ref uri);
        }

        string hash;
        public string Hash
        {
            get => hash;
            set => Set(value, ref hash);
        }

        public void SetHash(byte[] bytes)
        {
            Hash = BitConverter.ToString(bytes).Replace("-", "");
        }

        public string DisplayName => Architecture + " - " + uri.AbsoluteUri;

        public bool Verify(IList<string> errors)
        {
            bool success = true;
            success &= VerifyArchitecture(errors);
            success &= VerifyKind(errors);
            success &= VerifyUrl(errors);
            success &= VerifyHash(errors);

            return success;
        }

        private bool VerifyArchitecture(IList<string> _)
        {
            // it's an enum
            return true;
        }

        private bool VerifyKind(IList<string> _)
        {
            // it's an enum
            return true;
        }

        private bool VerifyUrl(IList<string> errors)
        {
            if (uri == null)
            {
                errors.Add("Installer URL cannot be empty.");
                return false;
            }

            // TODO: Should this support FTP (and possibly other schemes)?
            if (!uri.IsWebUrl())
            {
                errors.Add("Installer URL must be http[s].");
                return false;

            }

            return true;
        }

        private bool VerifyHash(IList<string> errors)
        {
            if (string.IsNullOrEmpty(hash))
            {
                errors.Add("Installer Hash cannot be empty.");
                return false;
            }

            if (hash.Length != 64)
            {
                errors.Add($"Installer Hash is not a valid SHA256 hash.");
                return false;
            }

            return true;
        }
    }
}
