#region using

using Castle.ActiveRecord;
using Castle.Core.Logging;
using Castle.MicroKernel.Facilities;
using NHibernate;
using NHibernate.Mapping;
using Component = Castle.MicroKernel.Registration.Component;
using ILoggerFactory = Castle.Core.Logging.ILoggerFactory;

#endregion

namespace Dry.Common.ActiveRecord {
    public class ARFacility : AbstractFacility {
        ILogger _log = NullLogger.Instance;
        const string ComponentName = "activerecord.sessionfactoryholder";
        const string SessionFactoryComponentName = "activerecord.sessionfactory";

        protected override void Init() {
            if (Kernel.HasComponent(typeof (ILoggerFactory))) {
                _log = Kernel.Resolve<ILoggerFactory>().Create(GetType());
            }

            AR.OnSessionFactoryCreated += (sf, cfg, name) => {
                var sfname = SessionFactoryComponentName;
                sfname += string.IsNullOrEmpty(name) ? string.Empty : "." + name;

                _log.Info(string.Format("Registering SessionFactory named '{0}': {1}", sfname, sf));
                Kernel.Register(Component.For<ISessionFactory>().Named(sfname).Instance(sf));
            };

            Castle.ActiveRecord.Config.ActiveRecordSectionHandler.Instance.Initialize();
            _log.Info(string.Format("Registering SessionFactoryHolder named '{0}': {1}", ComponentName, AR.Holder));
            Kernel.Register(Component.For<ISessionFactoryHolder>().Named(ComponentName).Instance(AR.Holder));
        }
    }
}
