namespace APKToolUI.Services
{
    public interface IFilePickerService
    {
        string? OpenFile(string filter);
        string? OpenFolder();
    }
}
