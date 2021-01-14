using GeneratorApiLibrary;
using GeneratorAppMain.Device;
using GeneratorAppMain.Messages;
using Unity;

namespace GeneratorAppMain
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
