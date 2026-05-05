using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using Windows.Graphics;

namespace CSD
{
    /// <summary>
    /// Initialization window shown when the app starts.
    /// </summary>
    public sealed partial class InitializationWindow : Window
    {
        private const string TokenSettingsKey = "Token";

        public InitializationWindow()
        {
            InitializeComponent();

            RestoreWindowState();
            Closed += (sender, args) => SaveWindowState();
        }

        private void RestoreWindowState()
        {
            try
            {
                var settings = AppSettings.Values;

                if (settings.ContainsKey("InitWindow_Width") && settings.ContainsKey("InitWindow_Height"))
                {
                    int width = Math.Max(400, (int)(double)settings["InitWindow_Width"]);
                    int height = Math.Max(300, (int)(double)settings["InitWindow_Height"]);
                    this.AppWindow.Resize(new SizeInt32(width, height));
                }

                if (settings.ContainsKey("InitWindow_X") && settings.ContainsKey("InitWindow_Y"))
                {
                    int x = (int)(double)settings["InitWindow_X"];
                    int y = (int)(double)settings["InitWindow_Y"];
                    this.AppWindow.Move(new PointInt32(x, y));
                }

                if (settings.ContainsKey("InitWindow_State"))
                {
                    string? state = settings["InitWindow_State"] as string;
                    if (state == "Maximized" && this.AppWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.Maximize();
                    }
                }
            }
            catch { }
        }

        private void SaveWindowState()
        {
            try
            {
                var settings = AppSettings.Values;
                settings["InitWindow_X"] = (double)this.AppWindow.Position.X;
                settings["InitWindow_Y"] = (double)this.AppWindow.Position.Y;
                settings["InitWindow_Width"] = (double)this.AppWindow.Size.Width;
                settings["InitWindow_Height"] = (double)this.AppWindow.Size.Height;

                if (this.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    settings["InitWindow_State"] = presenter.State.ToString();
                }
            }
            catch { }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TokenBox.Password))
            {
                return;
            }

            AppSettings.Values[TokenSettingsKey] = TokenBox.Password;

            var mainWindow = new MainWindow();
            mainWindow.Activate();
            Close();
        }

        private void TutorialButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://520.re/csh",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }
        }
    }
}
