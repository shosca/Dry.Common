#region using

using System;
using System.Collections;
using System.Reflection;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;

#endregion

namespace Dry.Common {
    public static class IoC {
        public static event EventHandler OnPreInitialize;
        public static event EventHandler OnPostInitialize;

        static IWindsorContainer _container;

        public static IWindsorContainer Container {
            get {
                if (_container == null)
                    Initialize();
                return _container;
            }
        }

        internal static void Initialize() {
            if (_container != null) return;

            InvokeOnPreInitialize(new EventArgs());
            _container = new WindsorContainer();
            _container.Install(
                Configuration.FromAppConfig()
                );
            InvokeOnPostInitialize(new EventArgs());
        }

        public static void InvokeOnPreInitialize(EventArgs e) {
            var handler = OnPreInitialize;
            if (handler != null) handler(null, e);
        }

        static void InvokeOnPostInitialize(EventArgs e) {
            var handler = OnPostInitialize;
            if (handler != null) handler(null, e);
        }

        public static void Install(Assembly assembly) {
            Container.Install(FromAssembly.Instance(assembly));
        }

        public static void Install(string assemblyname) {
            Container.Install(FromAssembly.Named(assemblyname));
        }

        public static void Install(IWindsorInstaller installer) {
            Container.Install(installer);
        }

        public static void Register(params IRegistration[] registerations) {
            Container.Register(registerations);
        }

        public static object Resolve(Type serviceType) {
            return Container.Resolve(serviceType);
        }

        public static object Resolve(Type serviceType, string serviceName) {
            return Container.Resolve(serviceName, serviceType);
        }

        public static T TryResolve<T>() {
            return TryResolve(default(T));
        }

        public static T TryResolve<T>(T defaultValue) {
            if (Container.Kernel.HasComponent(typeof (T)) == false)
                return defaultValue;
            return Container.Resolve<T>();
        }

        public static T Resolve<T>() {
            return Container.Resolve<T>();
        }

        public static T Resolve<T>(string name) {
            return Container.Resolve<T>(name);
        }

        public static T Resolve<T>(object argumentsAsAnonymousType) {
            return Container.Resolve<T>(argumentsAsAnonymousType);
        }

        public static T Resolve<T>(IDictionary parameters) {
            return Container.Resolve<T>(parameters);
        }

        public static Array ResolveAll(Type service) {
            return Container.ResolveAll(service);
        }

        public static T[] ResolveAll<T>() {
            return Container.ResolveAll<T>();
        }

        public static void Release(object instance) {
            if (instance == null) return;
            Container.Release(instance);
        }

        public static void Dispose() {
            Container.Dispose();
        }

        public static bool HasComponent<T>() {
            return Container.Kernel.HasComponent(typeof (T));
        }

        public static void ReleaseComponent(object component) {
            Container.Kernel.ReleaseComponent(component);
        }
    }
}
