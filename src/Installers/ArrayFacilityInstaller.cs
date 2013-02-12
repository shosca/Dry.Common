#region using

using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

#endregion

namespace Dry.Common.Installers {
    public class ArrayFacilityInstaller : IWindsorInstaller {
        public void Install(IWindsorContainer container, IConfigurationStore store) {
            container.Kernel.Resolver.AddSubResolver(new ArrayResolver(container.Kernel));
        }
    }
}
