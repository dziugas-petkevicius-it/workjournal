namespace WorkJournal
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage())
            {
                Title = "WorkJournal",
#if DEBUG
                Width = 1492,
                Height = 932
#endif
            };

            return window;
        }
    }
}
