using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace SiS.Communication.Demo
{
    public class AppConfig
    {
        private AppConfig()
        {

        }
        public static AppConfig Singleton { private set; get; }

        #region Properties

        public string HttpRootDir { get; set; }
        public int HttpListenPort { get; set; } = 8080;
        public bool EnableGZIP { get; set; } = true;
        #endregion

        public static void Load()
        {
            string cfgFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appConfig.json");
            if (!File.Exists(cfgFilePath))
            {
                Singleton = new AppConfig();
            }
            else
            {
                try
                {
                    string strConfig = File.ReadAllText(cfgFilePath, Encoding.UTF8);
                    Singleton = JsonConvert.DeserializeObject<AppConfig>(strConfig);
                }
                catch
                {
                    Singleton = new AppConfig();
                }
            }
        }
        public static void Save()
        {
            string cfgFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appConfig.json");
            string strConfig = JsonConvert.SerializeObject(Singleton);
            File.WriteAllText(cfgFilePath, strConfig, Encoding.UTF8);
        }
    }
}
