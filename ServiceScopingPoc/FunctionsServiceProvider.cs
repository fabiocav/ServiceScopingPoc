using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceScopingPoc
{
    public class FunctionsServiceProvider : IServiceProvider, IServiceScopeFactory
    {
        private Container _root;
        private IContainer _currentResolver;
        private readonly IServiceCollection _rootDescriptors;

        public FunctionsServiceProvider(IServiceCollection descriptors)
        {
            _root = new Container(rules =>
            {
                return rules
                .With(FactoryMethod.ConstructorWithResolvableArguments)
                .WithFactorySelector(Rules.SelectLastRegisteredFactory())
                .WithTrackingDisposableTransients();
            });

            _root.Populate(descriptors);
            _currentResolver = _root;
            _root.UseInstance<IServiceProvider>(this);
            _root.UseInstance<FunctionsServiceProvider>(this);

            var sp = _currentResolver.Resolve<IServiceProvider>();
            _rootDescriptors = descriptors;
        }

        public string State { get; set; }

        public IServiceProvider ServiceProvider => throw new NotImplementedException();

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            if (serviceType == typeof(IServiceScopeFactory))
            {
                return this;
            }

            var service = _currentResolver.Resolve(serviceType, IfUnresolved.ReturnDefault);
            string name = serviceType.Name;

            return service;
        }

        public void AddServices(IServiceCollection services)
        {
            _root.Populate(services);
        }

        internal void UpdateChildServices(IServiceCollection serviceDescriptors)
        {
            var rules = Rules.Default
                .WithUnknownServiceResolvers(request => new DelegateFactory(_ => _root.Resolve(request.ServiceType, IfUnresolved.ReturnDefault)));

            var resolver = new Container(rules);
            resolver.Populate(serviceDescriptors);

            _currentResolver = resolver;
        }

        public IServiceScope CreateScope()
        {
            return new FunctionsServiceScope(_currentResolver.OpenScope());
        }

        public class FunctionsServiceScope : IServiceScope
        {
            private readonly ScopedServiceProvider _serviceProvider;

            public FunctionsServiceScope(IResolverContext serviceProvider)
            {
                _serviceProvider = new ScopedServiceProvider(serviceProvider);
            }

            public IServiceProvider ServiceProvider => _serviceProvider;

            public void Dispose()
            {
                _serviceProvider.Dispose();
            }
        }

        public class ScopedServiceProvider : IServiceProvider, IDisposable
        {
            private readonly IResolverContext _resolver;

            public ScopedServiceProvider(IResolverContext container)
            {
                _resolver = container;
            }

            public void Dispose()
            {
                _resolver.Dispose();
            }

            public object GetService(Type serviceType)
            {
                return _resolver.Resolve(serviceType, IfUnresolved.ReturnDefault);
            }
        }
    }
}
