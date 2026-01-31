using System;
using System.Windows.Data;
using System.Windows.Markup;
using PulseAPK.Services;

namespace PulseAPK.Utils
{
    [MarkupExtensionReturnType(typeof(string))]
    public class LocalizationExtension : MarkupExtension
    {
        public string Key { get; set; }

        public LocalizationExtension() { }

        public LocalizationExtension(string key)
        {
            Key = key ?? string.Empty;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return "#NO_KEY#";

            try
            {
                // Simple design-time fallback
                if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
                {
                    return Key;
                }

                var binding = new Binding
                {
                    Source = LocalizationService.Instance,
                    Path = new System.Windows.PropertyPath($"[{Key}]"),
                    Mode = BindingMode.OneWay
                };

                return binding.ProvideValue(serviceProvider);
            }
            catch
            {
                return Key; // Fallback
            }
        }
    }
}
