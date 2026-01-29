using Microsoft.Extensions.Logging;
#if ANDROID
using WorkJournal.Interface;
using WorkJournal.Platforms.Android;
#endif
using WorkJournal.Services;

namespace WorkJournal
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddLogging();

#if ANDROID
            builder.Services.AddSingleton<IFolderPicker, FolderPicker>();
#endif

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<AppBarTitleService>();

            return builder.Build();
        }
    }
}
