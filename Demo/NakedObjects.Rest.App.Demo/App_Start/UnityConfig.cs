using System;
using System.Linq;
using System.Security.Principal;
using System.Web;
using AdventureWorksModel;
using Microsoft.Practices.Unity;
using NakedObjects;
using NakedObjects.Architecture.Component;
using NakedObjects.Architecture.Facets;
using NakedObjects.Architecture.Persist;
using NakedObjects.Architecture.Reflect;
using NakedObjects.Architecture.Security;
using NakedObjects.Core.Adapter.Map;
using NakedObjects.Core.Context;
using NakedObjects.Core.Reflect;
using NakedObjects.Core.Security;
using NakedObjects.Core.spec;
using NakedObjects.EntityObjectStore;
using NakedObjects.Managers;
using NakedObjects.Objects;
using NakedObjects.Persistor;
using NakedObjects.Persistor.Objectstore;
using NakedObjects.Reflector.DotNet;
using NakedObjects.Reflector.DotNet.Facets;
using NakedObjects.Reflector.DotNet.Reflect;
using NakedObjects.Reflector.DotNet.Reflect.Strategy;
using NakedObjects.Service;
using NakedObjects.Surface;
using NakedObjects.Surface.Nof4.Implementation;
using NakedObjects.Surface.Nof4.Utility;

namespace RestfulObjects.Mvc.App.App_Start
{
    /// <summary>
    /// Specifies the Unity configuration for the main container.
    /// </summary>
    public class UnityConfig
    {
        #region Unity Container
        private static Lazy<IUnityContainer> container = new Lazy<IUnityContainer>(() =>
        {
            var container = new UnityContainer();
            RegisterTypes(container);
            return container;
        });

        /// <summary>
        /// Gets the configured Unity container.
        /// </summary>
        public static IUnityContainer GetConfiguredContainer()
        {
            return container.Value;
        }
        #endregion


        private static ServicesConfiguration ServicesConfiguration() {
            var config = new ServicesConfiguration();
            config.AddMenuServices(
                new CustomerRepository(),
                new OrderRepository(),
                new ProductRepository(),
                new EmployeeRepository(),
                new SalesRepository(),
                new SpecialOfferRepository(),
                new ContactRepository(),
                new VendorRepository(),
                new PurchaseOrderRepository(),
                new WorkOrderRepository());
            config.AddContributedActions(
                new OrderContributedActions(),
                new CustomerContributedActions());
            config.AddSystemServices();
            return config;
        }

        private static EntityObjectStoreConfiguration EntityObjectStore() {
            var config = new EntityObjectStoreConfiguration();
            config.UsingEdmxContext("Model").AssociateTypes(AdventureWorksTypes);
            config.SpecifyTypesNotAssociatedWithAnyContext(() => new[] { typeof(AWDomainObject) });
            return config;
        }

        private static Type[] AdventureWorksTypes() {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "AdventureWorksModel").GetTypes();
            return allTypes.Where(t => t.BaseType == typeof(AWDomainObject) && !t.IsAbstract).ToArray();
        }

        //TODO: The need for this (in addition to the ServicesConfiguration) should be removed.
        public static Type[] Services() {
            return new[] {
                typeof (CustomerRepository),
                typeof (OrderRepository),
                typeof (ProductRepository),
                typeof (EmployeeRepository),
                typeof (SalesRepository),
                typeof (SpecialOfferRepository),
                typeof (ContactRepository),
                typeof (VendorRepository),
                typeof (PurchaseOrderRepository),
                typeof (WorkOrderRepository),
                typeof (OrderContributedActions),
                typeof (CustomerContributedActions)
            };
        }


        /// <summary>Registers the type mappings with the Unity container.</summary>
        /// <param name="container">The unity container to configure.</param>
        /// <remarks>There is no need to register concrete types such as controllers or API controllers (unless you want to 
        /// change the defaults), as Unity allows resolving a concrete type even if it was not previously registered.</remarks>
        public static void RegisterTypes(IUnityContainer container)
        {
            container.RegisterInstance<IServicesConfiguration>(ServicesConfiguration(), new ContainerControlledLifetimeManager());
            container.RegisterInstance<IEntityObjectStoreConfiguration>(EntityObjectStore(), new ContainerControlledLifetimeManager());

            // in architecture
            container.RegisterType<IClassStrategy, DefaultClassStrategy>();
            container.RegisterType<IFacetFactorySet, FacetFactorySetImpl>();
            container.RegisterType<INakedObjectReflector, DotNetReflector>(new ContainerControlledLifetimeManager());
            container.RegisterType<IMetamodel, DotNetReflector>(new ContainerControlledLifetimeManager());
            container.RegisterType<IPocoAdapterMap, PocoAdapterHashMap>(new PerResolveLifetimeManager(), new InjectionConstructor(10));
            container.RegisterType<IIdentityAdapterMap, IdentityAdapterHashMap>(new PerResolveLifetimeManager(), new InjectionConstructor(10));
            container.RegisterType<IContainerInjector, DotNetDomainObjectContainerInjector>(new PerResolveLifetimeManager());
            container.RegisterType<IOidGenerator, EntityOidGenerator>(new PerResolveLifetimeManager());
            container.RegisterType<IPersistAlgorithm, EntityPersistAlgorithm>(new PerResolveLifetimeManager());
            container.RegisterType<INakedObjectStore, NakedObjects.EntityObjectStore.EntityObjectStore>(new PerResolveLifetimeManager());
            container.RegisterType<IIdentityMap, EntityIdentityMapImpl>(new PerResolveLifetimeManager());
            container.RegisterType<INakedObjectTransactionManager, ObjectStoreTransactionManager>(new PerResolveLifetimeManager());
            container.RegisterType<INakedObjectManager, NakedObjectManager>(new PerResolveLifetimeManager());
            container.RegisterType<IObjectPersistor, ObjectPersistor>(new PerResolveLifetimeManager());
            container.RegisterType<IServicesManager, ServicesManager>(new PerResolveLifetimeManager());
            container.RegisterType<IAuthorizationManager, NullAuthorizationManager>(new PerResolveLifetimeManager());
            container.RegisterType<ILifecycleManager, LifeCycleManager>(new PerResolveLifetimeManager());
            container.RegisterType<IMetamodelManager, MetamodelManager>(new PerResolveLifetimeManager());
            container.RegisterType<ISession, WindowsSession>(new PerResolveLifetimeManager());
            container.RegisterType<IMessageBroker, SimpleMessageBroker>(new PerResolveLifetimeManager());
            container.RegisterType<INakedObjectsFramework, NakedObjectsFramework>(new PerResolveLifetimeManager());

            // surface
            container.RegisterType<IOidStrategy, ExternalOid>(new PerResolveLifetimeManager());
            container.RegisterType<INakedObjectsSurface, NakedObjectsSurface>(new PerResolveLifetimeManager());

            //Temporary scaffolding
            container.RegisterType<NakedObjectFactory, NakedObjectFactory>(new PerResolveLifetimeManager());
            container.RegisterType<MemberFactory, MemberFactory>(new PerResolveLifetimeManager());
            container.RegisterType<IUpdateNotifier, SimpleUpdateNotifier>(new PerResolveLifetimeManager());

            //Externals
            container.RegisterType<IPrincipal>(new InjectionFactory(c => HttpContext.Current.User));
        }
    }
}
