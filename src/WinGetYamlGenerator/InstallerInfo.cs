
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;

namespace WinGetYamlGenerator
{
    public enum ArchitectureKind
    {
        x86,
        x64,
        ARM,
        ARM64,
        Neutral,
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

        string language;
        public string Language
        {
            get => language;
            set => Set(value, ref language);
        }

        public void SetHash(byte[] bytes)
        {
            Hash = BitConverter.ToString(bytes).Replace("-", "");
        }

        internal void CopyFrom(InstallerInfo other)
        {
            arch = other.arch;
            kind = other.kind;
            uri = other.uri;
            hash = other.hash;
            language = other.language;

            RaisePropertyChanged(string.Empty);
        }

        internal InstallerInfo Clone()
        {
            var clone = new InstallerInfo();
            clone.CopyFrom(this);
            return clone;
        }

        public string DisplayName => Architecture + " - " + uri.AbsoluteUri;

        public bool Verify(IList<string> errors)
        {
            bool success = true;
            success &= VerifyArchitecture(errors);
            success &= VerifyKind(errors);
            success &= VerifyLanguage(errors);
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

        private bool VerifyLanguage(IList<string> errors)
        {
            if (string.IsNullOrEmpty(language))
            {
                // optional
                return true;
            }

            try
            {
                var _ = new CultureInfo(language);
            }
            catch
            {
                errors.Add($"{language} is not a valid BCP-47 language code.");
                return false;
            }

            return true;
        }

        private bool VerifyUrl(IList<string> errors)
        {
            if (uri == null)
            {
                errors.Add("Installer URL cannot be empty.");
                return false;
            }

            // Per spec, it must be HTTPS
            if (!uri.IsSecureWebUrl())
            {
                errors.Add("Installer URL must be https.");
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
