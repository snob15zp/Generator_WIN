using GeneratorApiLibrary;
using GeneratorWindowsApp.Device;
using GeneratorWindowsApp.Messages;
using GenLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace GeneratorWindowsApp
{
    public static class UnityConfiguration
    {
        static private UnityContainer container = new UnityContainer();

        public static void RegisterComponents()
        {
            container.RegisterType<IDeviceConnectionFactory, DeviceConnectionFactory>(TypeLifetime.Singleton);
            container.RegisterType<IDeviceManager, DeviceManager>(TypeLifetime.Singleton);
            container.RegisterFactory<IGeneratorApi>(f => new GeneratorApi(Properties.Settings.Default.baseApiUrl));
            container.RegisterType<IMessageHandler, MessageHandler>(TypeLifetime.Singleton);
            container.RegisterType<IMessageServer, MessageServer>(TypeLifetime.Singleton);
        }

        public static T Resolve<T>()
        {
            return container.Resolve<T>();
        }
    }
}
