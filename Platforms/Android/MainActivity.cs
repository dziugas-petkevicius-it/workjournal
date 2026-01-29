using Android.App;
using Android.Content;
using Android.Content.PM;

namespace WorkJournal
{
    [Activity(ScreenOrientation = ScreenOrientation.Landscape, Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private TaskCompletionSource<string?>? _tcsFolder;

        public Task<string?> PickFolderAsync()
        {
            _tcsFolder = new TaskCompletionSource<string?>();

            var intent = new Intent(Intent.ActionOpenDocumentTree);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

            StartActivityForResult(intent, 1001);

            return _tcsFolder.Task;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == 1001)
            {
                if (resultCode == Result.Ok && data != null)
                {
                    var uri = data.Data;
                    _tcsFolder?.SetResult(uri?.ToString());
                }
                else
                {
                    _tcsFolder?.SetResult(null);
                }
            }
        }
    }
}
