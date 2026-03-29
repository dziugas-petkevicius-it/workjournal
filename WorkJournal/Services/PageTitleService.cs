namespace WorkJournal.Services;

public class PageTitleService : IPageTitleService
{
    private string? _pageTitle;

    public string GetPageTitle() => _pageTitle ?? string.Empty;

    public void SetPageTitle(string title) => _pageTitle = title;
}
