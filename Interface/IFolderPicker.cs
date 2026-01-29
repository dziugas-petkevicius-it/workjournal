namespace WorkJournal.Interface;

public interface IFolderPicker
{
    Task<string?> PickFolderAsync();
}
