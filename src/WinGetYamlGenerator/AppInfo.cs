using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace WinGetYamlGenerator
{
    public class AppInfo : PropertyChangedBase
    {
        string id;
        public string Id
        {
            get => id;
            set => Set(value, ref id);
        }

        string publisher;
        public string Publisher
        {
            get => publisher;
            set => Set(value, ref publisher);
        }

        string name;
        public string Name
        {
            get => name;
            set
            {
                Set(value, ref name);
                if (value == "lorum")
                {
                    FillInTestData();
                }
            }
        }

        string moniker;
        public string Moniker
        {
            get => moniker;
            set => Set(value, ref moniker);
        }

        string tags;
        public string Tags
        {
            get => tags;
            set => Set(value, ref tags);
        }

        string desription;
        public string Description
        {
            get => desription;
            set => Set(value, ref desription);
        }

        Version version;
        public Version Version
        {
            get => version;
            set => Set(value, ref version);
        }

        string license;
        public string License
        {
            get => license;
            set => Set(value, ref license);
        }

        Uri licenseUri;
        public Uri LicenseUri
        {
            get => licenseUri;
            set => Set(value, ref licenseUri);
        }

        Uri homepage;
        public Uri Homepage
        {
            get => homepage;
            set => Set(value, ref homepage);
        }

        InstallerKind installerKind;
        public InstallerKind InstallerKind
        {
            get => installerKind;
            set => Set(value, ref installerKind);
        }

        public ObservableCollection<InstallerInfo> Installers { get; } = new ObservableCollection<InstallerInfo>();

        public bool Verify(IList<string> errors)
        {
            bool success = true;
            success &= VerifyName(errors);
            success &= VerifyPublisher(errors);
            success &= VerifyId(errors);
            success &= VerifyMoniker(errors);
            success &= VerifyTags(errors);
            success &= VerifyDescription(errors);
            success &= VerifyHomepage(errors);
            success &= VerifyVersion(errors);
            success &= VerifyLicense(errors);
            success &= VerifyLicenseUrl(errors);
            success &= VerifyInstallerKind(errors);
            success &= VerifyInstallers(errors);

            return success;
        }

        private bool VerifyName(IList<string> errors)
        {
            if (string.IsNullOrEmpty(name))
            {
                errors.Add("Name cannot be empty.");
                return false;
            }

            return true;
        }

        private bool VerifyPublisher(IList<string> errors)
        {
            if (string.IsNullOrEmpty(publisher))
            {
                errors.Add("Publisher cannot be empty.");
                return false;
            }

            return true;
        }

        private bool VerifyId(IList<string> errors)
        {
            if (string.IsNullOrEmpty(id))
            {
                errors.Add("ID cannot be empty.");
                return false;
            }

            if (id.Contains(" "))
            {
                errors.Add("ID must not contain spaces.");
                return false;
            }

            if (!id.Contains("."))
            {
                errors.Add("ID should be in the form 'publisher.appname[...].");
                return false;
            }

            return true;
        }

        private bool VerifyMoniker(IList<string> errors)
        {
            // optional
            if (string.IsNullOrEmpty(moniker))
            {
                return true;
            }

            if (moniker.Contains(" "))
            {
                errors.Add("Alternate name (moniker) must not contain spaces.");
                return false;
            }

            return true;
        }

        private bool VerifyTags(IList<string> _)
        {
            // optional
            return true;
        }

        private bool VerifyDescription(IList<string> _)
        {
            // optional
            return true;
        }

        private bool VerifyHomepage(IList<string> errors)
        {
            if (homepage == null)
            {
                // optional
                return true;
            }

            if (!homepage.IsWebUrl())
            {
                errors.Add("Homepage URL must be http[s].");
                return false;
            }

            return true;
        }

        private bool VerifyVersion(IList<string> errors)
        {
            if (version == null)
            {
                errors.Add("Version cannot be empty.");
                return false;
            }

            if (version.IsVersionZero())
            {
                errors.Add("Version number must be greater than 0.");
                return false;
            }

            return true;
        }

        private bool VerifyLicense(IList<string> errors)
        {
            if (string.IsNullOrEmpty(license))
            {
                errors.Add("License cannot be empty.");
                return false;
            }

            return true;
        }

        private bool VerifyLicenseUrl(IList<string> errors)
        {
            if (licenseUri == null)
            {
                // Optional
                return true;
            }

            if (!licenseUri.IsWebUrl())
            {
                errors.Add($"License URL must be http[s].");
                return false;
            }

            return true;
        }

        private bool VerifyInstallerKind(IList<string> _)
        {
            // it's an enum
            return true;
        }

        private bool VerifyInstallers(IList<string> errors)
        {
            if (Installers == null || Installers.Count == 0)
            {
                errors.Add("Must have at least one installer.");
                return false;
            }

            var success = true;
            foreach (var installer in Installers)
            {
                success &= installer.Verify(errors);
            }

            return success;
        }

        public void GenerateSuggestedAppId()
        {
            if (string.IsNullOrEmpty(publisher) || string.IsNullOrEmpty(name))
            {
                return;
            }

            var badChars = new Regex(@"[^\w]+");
            var suggestion = $"{badChars.Replace(publisher.Trim(), "")}.{badChars.Replace(name.Trim(), "")}";

            Id = suggestion;
        }

        public string GenerateYaml()
        {
            return GenerateYaml(true);
        }

        public string GenerateYaml(bool checkValidity)
        {
            if (checkValidity && !Verify(new List<string>()))
            {
                throw new InvalidOperationException("Cannot generate YAML for invalid or incomplete data");
            }

            var yaml = $@"Id: {Id}
Publisher: {Publisher}
Name: {Name}
Version: {Version?.ToCanonicalVersion().ToString(4)}
License: {License}
InstallerType: {InstallerKind}
";

            if (LicenseUri != null)
            {
                yaml += $"LicenseUrl: {LicenseUri.AbsoluteUri}{Environment.NewLine}";
            }

            if (Moniker != null)
            {
                yaml += $"AppMoniker: {Moniker}{Environment.NewLine}";
            }

            if (Tags != null)
            {
                yaml += $"Tags: {Tags}{Environment.NewLine}";
            }

            if (Description != null)
            {
                yaml += $"Description: {Description}{Environment.NewLine}";
            }

            if (Homepage != null)
            {
                yaml += $"Homepage: {Homepage.AbsoluteUri}{Environment.NewLine}";
            }

            yaml += $"Installers:{Environment.NewLine}";

            foreach (var installer in Installers)
            {
                yaml += $@"  - Arch: {installer.Architecture}
    InstallerType: {installer.InstallerKind}
    Url: {installer.Uri?.AbsoluteUri}
    Sha256: {installer.Hash}
";

                if (!string.IsNullOrEmpty(installer.Language))
                {
                    yaml += $"    Language: {installer.Language}{Environment.NewLine}";
                }

                if (!string.IsNullOrEmpty(installer.SilentSwitch))
                {
                    yaml += $"    Silent: {installer.SilentSwitch}{Environment.NewLine}";
                }
            }

            yaml += $"# Generated by https://github.com/ptorr-msft/WinGetYamlGenerator{Environment.NewLine}";
            return yaml;
        }

        private void FillInTestData()
        {
            Name = "Windows Package Manager YAML Generator";
            Publisher = "Peter Torr";
            GenerateSuggestedAppId();
            Moniker = "wingetpg";
            Tags = "winget,yaml";
            Description = "Generates YAML for the Windows Package Manager.";
            Homepage = new Uri("https://github.com/ptorr-msft/WinGetYamlGenerator");
            Version = App.CurrentVersion;
            License = "MIT";
            LicenseUri = new Uri("https://github.com/ptorr-msft/WinGetYamlGenerator/blob/master/LICENSE");
            InstallerKind = InstallerKind.MSIX;

            Installers.Add(new InstallerInfo { Architecture = ArchitectureKind.x86, Uri = new Uri("https://www.contoso.com/download/x86"), Hash = new string('A', 64), InstallerKind = InstallerKind.MSIX });
            Installers.Add(new InstallerInfo { Architecture = ArchitectureKind.x64, Uri = new Uri("https://www.contoso.com/download/x64"), Hash = new string('B', 64), InstallerKind = InstallerKind.EXE });
        }
    }
}
