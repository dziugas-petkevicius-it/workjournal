namespace WorkJournal.Services;

public class AppBarTitleService
{
    private string _title = string.Empty;
    public event Action<string>? OnTitleChanged;

    public string Title
    {
        get => _title;
        private set
        {
            if (_title != value)
            {
                _title = value;
                OnTitleChanged?.Invoke(_title);
            }
        }
    }

    public void SetTitle(string title)
    {
        Title = title;
    }
}

