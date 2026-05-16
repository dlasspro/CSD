using Microsoft.UI.Xaml;
using System.Net.Http;


using CSD.Views;
using CSD.Models;
using CSD.Services;
using CSD.Helpers;
using CSD.Settings;


namespace CSD.Settings
{
    public class SettingsContext
    {
        public Window Window { get; }
        public HttpClient HttpClient { get; }
        
        public SettingsContext(Window window, HttpClient httpClient)
        {
            Window = window;
            HttpClient = httpClient;
        }
    }
}