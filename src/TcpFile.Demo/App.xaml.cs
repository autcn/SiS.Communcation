using SiS.Communication.Business;
using System.Reflection;
using System.Windows;

namespace TcpFile.Demo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            TcpModelConverter.Initialize(JsonSerializer.Default, typeRegister =>
            {
                typeRegister.Register(Assembly.GetExecutingAssembly());
            });
            base.OnStartup(e);
        }
    }
}
