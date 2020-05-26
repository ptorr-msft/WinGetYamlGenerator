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
                currentlyEditingInstallerInfo = value;
                RaisePropertyChanged();
            }
        }

        public MainPage()
        {
            InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(780, 725);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            StartupCheck();
        }

        private async void StartupCheck()
        {
            if (!string.IsNullOrEmpty(App.InitError))
            {
                await ShowDialog("Startup error", App.InitError);
            }
        }

        void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task ShowErrors(IList<string> errors)
        {
            var builder = new StringBuilder($"Error(s) were found; please correct them before trying again:{Environment.NewLine}{Environment.NewLine}");
            foreach (var e in errors)
            {
                builder.Append(e);
                builder.Append(Environment.NewLine);
            }

            await ShowDialog("Errors in form", builder.ToString());
        }

        private void ShowDialogErrors(string text)
        {
            ErrorPanelRow.Height = GridLength.Auto;
            ErrorPanelText.Text = text;
        }

        private void HideDialogErrors()
        {
            ErrorPanelRow.Height = new GridLength(0);
            ErrorPanelText.Text = String.Empty;
        }

        private async void DownloadInstaller(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button downloadButton))
            {
                return;
            }

            try
            {
                downloadButton.IsEnabled = false;

                if (CurrentlyEditingInstallerInfo.Uri == null)
                {
                    return;
                }

                if (!CurrentlyEditingInstallerInfo.Uri.IsWebUrl())
                {
                    ShowDialogErrors($"Cannot download from {CurrentlyEditingInstallerInfo.Uri.AbsoluteUri}'.");
                    return;
                }

                var infoFlyout = new Flyout
                {
                    Content = new TextBlock
                    {
                        Text = "Launching browser..."
                    }
                };
                infoFlyout.ShowAt(downloadButton);
                await Launcher.LaunchUriAsync(CurrentlyEditingInstallerInfo.Uri);
            }
            catch (Exception ex)
            {
                ShowDialogErrors($"Error trying to launch browser: {ex.Message}{Environment.NewLine}Maybe try again later?");
            }
            finally
            {
                downloadButton.IsEnabled = true;
            }
        }

        private async void GenerateHashFromFile(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button generateButton))
            {
                return;
            }

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
                picker.FileTypeFilter.Add(".msixbundle");

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
                ShowDialogErrors($"Error trying to calculate hash: {ex.Message}{Environment.NewLine}Maybe try again later?");
            }
            finally
            {
                generateButton.IsEnabled = true;
            }
        }

        private async void RemoveInstallerWithUi(object sender, RoutedEventArgs e)
        {
            if (!(((Button)sender).DataContext is InstallerInfo data))
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Confirm removal",
                Content = $"Do you want to remove the {data.Architecture} installer?",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                IsPrimaryButtonEnabled = true
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            AppInfo.Installers.Remove(data);
        }

        private async void AddNewInstallerWithUi(object sender, RoutedEventArgs e)
        {
            HideDialogErrors();

            var installer = new InstallerInfo
            {
                InstallerKind = AppInfo.InstallerKind
            };

            CurrentlyEditingInstallerInfo = installer;
            await InstallerPopup.ShowAsync();
        }

        private async void SaveAsFile(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button saveButton))
            {
                return;
            }

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
                await ShowDialog("Success", $"Saved as {picker.SuggestedFileName}.");
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", $"Error saving file: {ex.Message}{Environment.NewLine}{Environment.NewLine}Maybe try again later?");
            }
            finally
            {
                saveButton.IsEnabled = true;
            }
        }

        private async void CopyToClipboard(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button copyButton))
            {
                return;
            }

            try
            {
                copyButton.IsEnabled = false;

                var errors = new List<string>();
                bool valid = AppInfo.Verify(errors);
                if (!valid && !chkIgnoreErrors.IsChecked.GetValueOrDefault())
                {
                    await ShowErrors(errors);
                    return;
                }

                string yaml = AppInfo.GenerateYaml(false);

                DataPackage package = new DataPackage();
                package.SetText(yaml);
                Clipboard.SetContent(package);

                var infoFlyout = new Flyout
                {
                    Content = new TextBlock
                    {
                        Text = $"Copied{(valid ? "" : " with error(s)")}"
                    }
                };
                infoFlyout.ShowAt(btnCopy);
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", $"Error copying to clipboard: {ex.Message}{Environment.NewLine}{Environment.NewLine}Maybe try again later?");
            }
            finally
            {
                copyButton.IsEnabled = true;
            }
        }

        public string CurrentVersion => $"App v{App.CurrentVersion}";

        private async Task<ContentDialogResult> ShowDialog(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Close"
            };

            return await dialog.ShowAsync();
        }

        private void CompleteAddInstaller(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var errors = new List<string>();
            if (!CurrentlyEditingInstallerInfo.Verify(errors))
            {
                var builder = new StringBuilder();
                foreach (var e in errors)
                {
                    builder.Append(e);
                    builder.Append(Environment.NewLine);
                }

                ShowDialogErrors(builder.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
                args.Cancel = true;
            }
            else
            {
                HideDialogErrors();
                AppInfo.Installers.Add(CurrentlyEditingInstallerInfo);
                CurrentlyEditingInstallerInfo = null;
            }
        }
    }
}