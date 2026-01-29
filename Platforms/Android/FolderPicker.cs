using WorkJournal.Interface;

namespace WorkJournal.Platforms.Android;
public class FolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync()
    {
        var activity = Platform.CurrentActivity as MainActivity;
        if (activity == null)
            return Task.FromResult<string?>(null);

        return activity.PickFolderAsync();
    }
}
