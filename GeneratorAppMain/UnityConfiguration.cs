using GeneratorApiLibrary;
using GeneratorAppMain.Device;
using GeneratorAppMain.Messages;
using GeneratorAppMain.Properties;
using Unity;

namespace GeneratorAppMain
{
    public static class UnityConfiguration
    {
        private static readonly UnityContainer container = new UnityContainer();

        public static void RegisterComponents()
        {
            container.RegisterType<IDeviceConnectionFactory, DeviceConnectionFactory>(TypeLifetime.Singleton);
            container.RegisterType<IDeviceManager, DeviceManager>(TypeLifetime.Singleton);
            container.RegisterFactory<IGeneratorApi>(f => new GeneratorApi(Settings.Default.baseApiUrl));
            container.RegisterType<IMessageHandler, MessageHandler>(TypeLifetime.Singleton);
            container.RegisterType<IMessageServer, MessageServer>(TypeLifetime.Singleton);
            container.RegisterType<IUsbDeviceDetector, UsbDeviceDetector>(TypeLifetime.Singleton);
           
        }

        public static T Resolve<T>()
        {
            return container.Resolve<T>();
        }
    }
}