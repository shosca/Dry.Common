#region using

using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.MonoRail.WindsorExtension;
using Castle.Windsor;
using Dry.Common.Queries;
using Dry.Common.Cache;

#endregion

namespace Dry.Common.Installers {
    public class WindsorInstaller : IWindsorInstaller {
        public void Install(IWindsorContainer container, IConfigurationStore store) {
            container.AddFacility<MonoRailFacility>();
            container.Register(
                new MrInstaller(),
                AllTypes.FromThisAssembly().BasedOn(typeof(IQuery<>)).LifestyleTransient(),
                Component.For<ICache>().ImplementedBy<MemcachedCache>()
            );
        }
    }
}
