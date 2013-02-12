#region using

using System.Reflection;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MonoRail.Framework;
using Dry.Common.Monorail;
using Dry.Common.Monorail.DynamicActions;
using Dry.Common.Monorail.Search;

#endregion

namespace Dry.Common.Installers {
    public class MrInstaller : IRegistration {
        public Assembly Assembly { get; private set; }

        public MrInstaller() {
            Assembly = Assembly.GetCallingAssembly();
        }

        public MrInstaller(Assembly assembly) {
            Assembly = assembly;
        }

        public void Register(IKernelInternal kernel) {
            kernel.Register(
                AllTypes.FromAssembly(Assembly).BasedOn<IController>()
                    .LifestyleTransient(),
                AllTypes.FromAssembly(Assembly).BasedOn<ViewComponent>()
                    .LifestyleTransient(),
                AllTypes.FromAssembly(Assembly).BasedOn<IFilter>()
                    .LifestyleTransient(),
                AllTypes.FromAssembly(Assembly).BasedOn<IDynamicActionProvider>()
                    .LifestyleTransient(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPreBind<>))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPostBind<>))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPreList))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPostList<>))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPreSave<>))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPostSave<>))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPreCreate<>))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPostCreate<>))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPreUpdate<>))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPostUpdate<>))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPreView))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(IPostView<>))
                    .LifestylePerWebRequest().WithServiceAllInterfaces(),
                AllTypes.FromAssembly(Assembly).BasedOn(typeof(ISiteSearch))
                    .LifestylePerWebRequest().WithServiceAllInterfaces()
            );
            Routing.Register(Assembly);
        }
    }
}
