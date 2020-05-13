using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WinGetYamlGenerator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(780, 500);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        bool VerifyAllFields(IList<string> errors)
        {
            bool success = true;
            success &= VerifyId(errors);
            success &= VerifyPublisher(errors);
            success &= VerifyName(errors);
            success &= VerifyVersion(errors);
            success &= VerifyLicense(errors);
            success &= VerifyLicenseUrl(errors);
            success &= VerifyInstallerUrlx64(errors);
            success &= VerifyInstallerKindx64(errors);
            success &= VerifyInstallerHashx64(errors);

            return success;
        }

        private bool VerifyLicense(IList<string> errors)
        {
            var license = txtLicense.Text;
            if (string.IsNullOrEmpty(license))
            {
                errors.Add("License cannot be empty.");
                return false;
            }

            return true;
        }

        private bool VerifyInstallerKindx64(IList<string> errors)
        {
            var kind = txtInstallerTypex64.Text;
            if (string.IsNullOrEmpty(kind))
            {
                // Optional
                return true;
            }

            kind = kind.ToLowerInvariant();
            if (kind != "msi" && kind != "msix" && kind != "exe")
            {
                errors.Add("Installer Type must be 'MSI', 'MSIX', or 'EXE'");
                return false;
            }

            return true;
        }

        private bool VerifyInstallerUrlx64(IList<string> errors)
        {
            var uri = txtInstallerUrlx64.Text;
            if (string.IsNullOrEmpty(uri))
            {
                errors.Add("Installer URL cannot be empty.");
                return false;
            }

            if (!Uri.TryCreate(uri, UriKind.Absolute, out var _))
            {
                errors.Add($"Installer URL '{uri}' is not a valid URL.");
                return false;
            }

            return true;
        }

        private bool VerifyInstallerHashx64(IList<string> errors)
        {
            var hash = txtInstallerHashx64.Text;
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

        private bool VerifyLicenseUrl(IList<string> errors)
        {
            var uri = txtLicenseUrl.Text;
            if (string.IsNullOrEmpty(uri))
            {
                // Optional
                return true;
            }

            if (!Uri.TryCreate(uri, UriKind.Absolute, out var _))
            {
                errors.Add($"License URL '{uri}' is not a valid URL.");
                return false;
            }

            return true;
        }

        private bool VerifyName(IList<string> errors)
        {
            var name = txtName.Text;
            if (string.IsNullOrEmpty(name))
            {
                errors.Add("Name cannot be empty.");
                return false;
            }

            return true;
        }

        bool VerifyId(IList<string> errors)
        {
            var id = txtId.Text;
            if (string.IsNullOrEmpty(id))
            {
                errors.Add("ID cannot be empty.");
                return false;
            }

            if (id.Contains(" "))
            {
                errors.Add("ID should not contain spaces.");
                return false;
            }

            if (!id.Contains("."))
            {
                errors.Add("ID should be in the form 'publisher.appname[.more_stuff]");
                return false;
            }

            return true;
        }

        bool VerifyPublisher(IList<string> errors)
        {
            var publisher = txtPublisher.Text;
            if (string.IsNullOrEmpty(publisher))
            {
                errors.Add("Publisher cannot be empty.");
                return false;
            }

            return true;
        }

        bool VerifyVersion(IList<string> errors)
        {
            var version = txtVersion.Text;
            if (string.IsNullOrEmpty(version))
            {
                errors.Add("Version cannot be empty.");
                return false;
            }

            if (!Version.TryParse(version, out var _))
            {
                errors.Add("Version must be in the form a.b.c.d.");
                return false;
            }

            return true;
        }

        private async void GenerateYaml(object sender, RoutedEventArgs e)
        {
            var errors = new List<string>();

            if (!VerifyAllFields(errors))
            {
                await ShowErrors(errors);
                return;
            }

            var yaml = $@"Id: {txtId.Text}
Publisher: {txtPublisher.Text}
Name: {txtName.Text}
Version: {txtVersion.Text}
License: {txtLicense.Text}
LicenseUrl: {txtLicenseUrl.Text}
Installers:
  - Arch: x64
    Url: {txtInstallerUrlx64.Text}
    InstallerType: {txtInstallerTypex64.Text}
    Sha256: {txtInstallerHashx64.Text}";

            var picker = new FileSavePicker();
            picker.SuggestedFileName = "thomas.yaml";
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeChoices.Add("YAML file", new[] { ".yaml" });
            var file = await picker.PickSaveFileAsync();
            await FileIO.WriteTextAsync(file, yaml);
            await (new MessageDialog("Success")).ShowAsync();
        }

        async Task ShowErrors(IList<string> errors)
        {
            StringBuilder builder = new StringBuilder($"Error(s) were found; please correct them before trying again:{Environment.NewLine}{Environment.NewLine}");
            foreach (var e in errors)
            {
                builder.Append(e);
                builder.Append(Environment.NewLine);
            }

            await (new MessageDialog(builder.ToString(), "Errors in form")).ShowAsync();
        }

        private async void DownloadAndCheckInstaller(object sender, RoutedEventArgs e)
        {
            if (!Uri.TryCreate(txtInstallerUrlx64.Text, UriKind.Absolute, out var uri))
            {
                await (new MessageDialog($"'{txtInstallerUrlx64.Text}' is not a valid URL.", "Cannot download")).ShowAsync();
                return;
            }

            await (new MessageDialog("Your browser will launch to download the file.", "Downloading app")).ShowAsync();
            await Launcher.LaunchUriAsync(uri);
        }

        private async void GenerateHashFromFile(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add("*");
            picker.FileTypeFilter.Add(".msi");
            picker.FileTypeFilter.Add(".msix");
            picker.FileTypeFilter.Add(".exe");
            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                var hasher = SHA256.Create();
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    var hashBytes = hasher.ComputeHash(stream);
                    var hashString = BitConverter.ToString(hashBytes).Replace("-", "");
                    txtInstallerHashx64.Text = hashString;
                }
            }
        }
    }
}
