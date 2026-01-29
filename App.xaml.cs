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
                Width = 429,   // width in DIPs
                Height = 929   // height in DIPs
#endif
            };

            return window;
        }
    }
}
