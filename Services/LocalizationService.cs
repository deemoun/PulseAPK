using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;
using PulseAPK.Properties;

namespace PulseAPK.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static readonly LocalizationService _instance = new();
        public static LocalizationService Instance => _instance;

        private readonly ResourceManager _resourceManager = Resources.ResourceManager;
        private CultureInfo _currentCulture = Thread.CurrentThread.CurrentUICulture;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string key]
        {
            get
            {
                var result = _resourceManager.GetString(key, _currentCulture);
                return result ?? $"#{key}#";
            }
        }

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    Thread.CurrentThread.CurrentUICulture = value;
                    Thread.CurrentThread.CurrentCulture = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                }
            }
        }

        public void SetLanguage(Languages language)
        {
            var cultureCode = language switch
            {
                Languages.Russian => "ru-RU",
                Languages.Ukrainian => "uk-UA",
                Languages.Spanish => "es-ES",
                _ => "en-US"
            };

            CurrentCulture = new CultureInfo(cultureCode);
        }
    }
}
