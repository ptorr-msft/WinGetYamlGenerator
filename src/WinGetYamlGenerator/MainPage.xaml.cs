using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
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
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        internal AppInfo AppInfo { get; } = new AppInfo();

        InstallerInfo currentlyEditingInstallerInfo;
        internal InstallerInfo CurrentlyEditingInstallerInfo
        {
            get => currentlyEditingInstallerInfo;
            set
            {
                currentlyEditingInstallerInfo = value; RaisePropertyChanged();
            }
        }

        public MainPage()
        {
            InitializeComponent();
            InstallerPopup.Visibility = Visibility.Collapsed;

            ApplicationView.PreferredLaunchViewSize = new Size(780, 725);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            if (!string.IsNullOrEmpty(App.InitError))
            {
                (new MessageDialog(App.InitError, "Startup error")).ShowAsync();
            }
        }

        void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        private async void DownloadInstaller(object sender, RoutedEventArgs e)
        {
            var downloadButton = sender as Button;
            try
            {
                downloadButton.IsEnabled = false;

                if (CurrentlyEditingInstallerInfo.Uri == null)
                {
                    return;
                }

                if (!CurrentlyEditingInstallerInfo.Uri.IsWebUrl())
                {
                    await (new MessageDialog($"Cannot download from {CurrentlyEditingInstallerInfo.Uri.AbsoluteUri}'.", "Cannot download")).ShowAsync();
                    return;
                }

                await (new MessageDialog("Your default browser will launch to download the file.", "Downloading app")).ShowAsync();
                await Launcher.LaunchUriAsync(CurrentlyEditingInstallerInfo.Uri);
            }
            catch (Exception ex)
            {
                await (new MessageDialog($"Error trying to launch browser: {ex.Message}{Environment.NewLine}Maybe try again later?", "Error")).ShowAsync();
            }
            finally
            {
                downloadButton.IsEnabled = true;
            }
        }

        private async void GenerateHashFromFile(object sender, RoutedEventArgs e)
        {
            var generateButton = sender as Button;

            try
            {
                generateButton.IsEnabled = false;
                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.Downloads
                };
                picker.FileTypeFilter.Add("*");
                picker.FileTypeFilter.Add(".exe");
                picker.FileTypeFilter.Add(".msi");
                picker.FileTypeFilter.Add(".msix");
                var file = await picker.PickSingleFileAsync();

                if (file != null)
                {
                    var hasher = SHA256.Create();
                    using (var stream = await file.OpenStreamForReadAsync())
                    {
                        var hashBytes = hasher.ComputeHash(stream);
                        var hashString = BitConverter.ToString(hashBytes).Replace("-", "");
                        CurrentlyEditingInstallerInfo.Hash = hashString;
                    }
                }
            }
            catch (Exception ex)
            {
                await (new MessageDialog($"Error trying to calculate hash: {ex.Message}{Environment.NewLine}Maybe try again later?", "Error")).ShowAsync();
            }
            finally
            {
                generateButton.IsEnabled = true;
            }

        }

        private async void CompleteAddInstaller(object sender, RoutedEventArgs e)
        {
            var errors = new List<string>();
            if (!CurrentlyEditingInstallerInfo.Verify(errors))
            {
                await ShowErrors(errors);
                return;
            }

            AppInfo.Installers.Add(CurrentlyEditingInstallerInfo);
            CurrentlyEditingInstallerInfo = null;
            InstallerPopup.Visibility = Visibility.Collapsed;
        }

        private void CancelAddInstaller(object sender, RoutedEventArgs e)
        {
            InstallerPopup.Visibility = Visibility.Collapsed;
        }

        private async void RemoveInstallerWithUI(object sender, RoutedEventArgs e)
        {
            var data = (sender as Button).DataContext as InstallerInfo;

            var dialog = new MessageDialog($"Do you want to remove the {data.Architecture} installer?", "Confirm removal");
            var delete = new UICommand("Yes");
            var cancel = new UICommand("No");
            dialog.Commands.Add(delete);
            dialog.Commands.Add(cancel);

            var result = await dialog.ShowAsync();
            if (result != delete)
            {
                return;
            }

            AppInfo.Installers.Remove(data);
        }

        private void AddNewInstallerWithUI(object sender, RoutedEventArgs e)
        {
            var installer = new InstallerInfo
            {
                InstallerKind = AppInfo.InstallerKind
            };

            InstallerPopup.Visibility = Visibility.Visible;
            CurrentlyEditingInstallerInfo = installer;
        }

        private async void SaveAsFile(object sender, RoutedEventArgs e)
        {
            var saveButton = sender as Button;

            var errors = new List<string>();
            if (!AppInfo.Verify(errors))
            {
                await ShowErrors(errors);
                return;
            }

            try
            {
                saveButton.IsEnabled = false;
                string yaml = AppInfo.GenerateYaml();
                var picker = new FileSavePicker
                {
                    SuggestedFileName = $"{AppInfo.Version.ToCanonicalVersion().ToString(4)}.yaml",
                    SuggestedStartLocation = PickerLocationId.Desktop
                };
                picker.FileTypeChoices.Add("YAML file", new[] { ".yaml" });
                var file = await picker.PickSaveFileAsync();

                if (file == null)
                {
                    return;
                }

                await FileIO.WriteTextAsync(file, yaml);
                await (new MessageDialog($"Saved as {picker.SuggestedFileName}.", "Success")).ShowAsync();
            }
            catch (Exception ex)
            {
                await (new MessageDialog($"Error saving file: {ex.Message}{Environment.NewLine}{Environment.NewLine}Maybe try again later?", "Error")).ShowAsync();
            }
            finally
            {
                saveButton.IsEnabled = true;
            }
        }

        private async void CopyToClipboard(object sender, RoutedEventArgs e)
        {
            var errors = new List<string>();
            if (!AppInfo.Verify(errors))
            {
                await ShowErrors(errors);
                return;
            }

            var copyButton = sender as Button;
            try
            {
                copyButton.IsEnabled = false;
                string yaml = AppInfo.GenerateYaml();

                DataPackage package = new DataPackage();
                package.SetText(yaml);
                Clipboard.SetContent(package);
            }
            catch (Exception ex)
            {
                await (new MessageDialog($"Error copying to clipboard: {ex.Message}{Environment.NewLine}{Environment.NewLine}Maybe try again later?", "Error")).ShowAsync();
            }
            finally
            {
                copyButton.IsEnabled = true;
            }
        }

        public string CurrentVersion => $"v{App.CurrentVersion}";
    }
}
