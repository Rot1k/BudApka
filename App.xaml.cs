using Microsoft.Extensions.DependencyInjection;

namespace BudApka
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            UserAppTheme =
                Microsoft.Maui.Storage.Preferences.Default.Get("budapka-theme", "light") == "dark"
                    ? AppTheme.Dark
                    : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
