using Microsoft.UI.Xaml;
using Windows.Storage;

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
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TokenBox.Password))
            {
                return;
            }

            ApplicationData.Current.LocalSettings.Values[TokenSettingsKey] = TokenBox.Password;

            var mainWindow = new MainWindow();
            mainWindow.Activate();
            Close();
        }
    }
}
