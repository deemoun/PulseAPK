namespace PulseAPK.Services
{
    public interface IFilePickerService
    {
        string? OpenFile(string filter);
        string? OpenFolder(string? initialDirectory = null);
    }
}
