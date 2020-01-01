using System.Windows;

namespace SiS.Communication.Demo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppConfig.Load();
            base.OnStartup(e);
        }
    }
}
