using System.Windows;

namespace PulseAPK.Utils
{
    public static class MessageBoxUtils
    {
        public static MessageBoxResult ShowInfo(string message, string? title = null)
        {
            return MessageBox.Show(message, title ?? Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static MessageBoxResult ShowWarning(string message, string? title = null)
        {
            return MessageBox.Show(message, title ?? Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static MessageBoxResult ShowError(string message, string? title = null)
        {
            return MessageBox.Show(message, title ?? Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static MessageBoxResult ShowQuestion(
            string message,
            string? title = null,
            MessageBoxButton buttons = MessageBoxButton.YesNo,
            MessageBoxImage icon = MessageBoxImage.Question,
            MessageBoxResult defaultResult = MessageBoxResult.No)
        {
            return MessageBox.Show(message, title ?? Properties.Resources.AppTitle, buttons, icon, defaultResult);
        }
    }
}
