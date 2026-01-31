using System;
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
        private ISettingsService? _settingsService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Initialize(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            
            // Load saved language preference
            if (!string.IsNullOrEmpty(_settingsService.Settings.SelectedLanguage))
            {
                try
                {
                    var savedCulture = new CultureInfo(_settingsService.Settings.SelectedLanguage);
                    CurrentCulture = savedCulture;
                }
                catch
                {
                    // If invalid culture, use default
                }
            }
        }

        public string this[string key]
        {
            get
            {
                // Try to get the string in the current culture
                var result = _resourceManager.GetString(key, _currentCulture);
                
                // If not found and not already English, fallback to English
                if (result == null && !_currentCulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase))
                {
                    result = _resourceManager.GetString(key, new CultureInfo("en-US"));
                }
                
                // If still not found, return placeholder
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
                    
                    // Save to settings
                    if (_settingsService != null)
                    {
                        _settingsService.Settings.SelectedLanguage = value.Name;
                        _settingsService.Save();
                    }
                    
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
                Languages.Chinese => "zh-CN",
                Languages.German => "de-DE",
                Languages.French => "fr-FR",
                Languages.Portuguese => "pt-BR",
                Languages.Arabic => "ar-SA",
                _ => "en-US"
            };

            CurrentCulture = new CultureInfo(cultureCode);
        }
    }
}
