using DevExpress.Xpf.Core;
using System.Windows;

namespace RfidReader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            SplashScreenManager.Create(() => new FluentSplashScreen() {}).ShowOnStartup();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ApplicationThemeHelper.UpdateApplicationThemeName();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ApplicationThemeHelper.UpdateApplicationThemeName();
        }
    }
}
