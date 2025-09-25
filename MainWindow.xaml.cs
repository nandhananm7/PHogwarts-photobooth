using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using WinForms = System.Windows.Forms;

namespace PhotoboothDesktop
{
    public partial class MainWindow : Window
    {
        private string _saveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Photobooth");

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(_saveFolder);

                await Web.EnsureCoreWebView2Async();

                var wwwroot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
                Directory.CreateDirectory(wwwroot);
                Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "appassets", wwwroot, CoreWebView2HostResourceAccessKind.DenyCors);

                Web.CoreWebView2.PermissionRequested += (s, args) =>
                {
                    if (args.PermissionKind == CoreWebView2PermissionKind.Camera ||
                        args.PermissionKind == CoreWebView2PermissionKind.Microphone)
                    {
                        args.State = CoreWebView2PermissionState.Allow;
                    }
                };

                Web.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                Web.CoreWebView2.DOMContentLoaded += (_, __) =>
                {
                    PostToWeb(new { type = "save-folder", path = _saveFolder });
                };

                Web.Source = new Uri("https://appassets/index.html");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to initialize WebView2: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try { Web?.CoreWebView2?.Stop(); Web?.Dispose(); } catch { }
        }

        private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.TryGetWebMessageAsString();
                var msg = System.Text.Json.JsonSerializer.Deserialize<WebMsg>(json);
                if (msg == null) return;

                switch (msg.type)
                {
                    case "save-gif":
                        var name = string.IsNullOrWhiteSpace(msg.name)
                            ? $"photobooth_{DateTime.Now:yyyyMMdd_HHmmss}.gif"
                            : msg.name;
                        var bytes = Convert.FromBase64String(msg.base64 ?? "");
                        Directory.CreateDirectory(_saveFolder);
                        var path = Path.Combine(_saveFolder, name);
                        await File.WriteAllBytesAsync(path, bytes);
                        PostToWeb(new { type = "save-result", ok = true, path });
                        break;

                    case "ask-folder":
                        PostToWeb(new { type = "save-folder", path = _saveFolder });
                        break;
                }
            }
            catch (Exception ex)
            {
                PostToWeb(new { type = "save-result", ok = false, error = ex.Message });
            }
        }

        private void PostToWeb(object payload)
        {
            var s = System.Text.Json.JsonSerializer.Serialize(payload);
            Web.CoreWebView2.PostWebMessageAsString(s);
        }

        private void PickFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new WinForms.FolderBrowserDialog
            {
                Description = "Choose a folder for Photobooth GIFs",
                UseDescriptionForTitle = true,
                SelectedPath = _saveFolder
            };
            if (dlg.ShowDialog() == WinForms.DialogResult.OK && Directory.Exists(dlg.SelectedPath))
            {
                _saveFolder = dlg.SelectedPath;
                Directory.CreateDirectory(_saveFolder);
                PostToWeb(new { type = "save-folder", path = _saveFolder });
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo { FileName = _saveFolder, UseShellExecute = true }); } catch { }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private class WebMsg
        {
            public string? type { get; set; }
            public string? name { get; set; }
            public string? base64 { get; set; }
        }
    }
}
