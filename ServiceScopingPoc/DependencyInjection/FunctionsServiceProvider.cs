using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace ServiceScopingPoc
{
    public class FunctionsServiceProvider : IServiceProvider, IServiceScopeFactory, IDisposable
    {
        private static readonly Rules _defaultContainerRules;
        private Container _root;
        private FunctionsResolver _currentResolver;

        static FunctionsServiceProvider()
        {
            _defaultContainerRules = Rules.Default
                .With(FactoryMethod.ConstructorWithResolvableArguments)
                .WithFactorySelector(Rules.SelectLastRegisteredFactory())
                .WithTrackingDisposableTransients();
        }

        public FunctionsServiceProvider(IServiceCollection descriptors)
        {
            _root = new Container(rules => _defaultContainerRules);

            _root.Populate(descriptors);
            _root.UseInstance<IServiceProvider>(this);
            _root.UseInstance<FunctionsServiceProvider>(this);

            _currentResolver = new FunctionsResolver(_root, true);
        }

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

            var service = _currentResolver.Container.Resolve(serviceType, IfUnresolved.ReturnDefault);
            string name = serviceType.Name;

            return service;
        }

        public void AddServices(IServiceCollection services)
        {
            _root.Populate(services);
        }

        internal void UpdateChildServices(IServiceCollection serviceDescriptors)
        {
            var rules = _defaultContainerRules
                .WithUnknownServiceResolvers(request => new DelegateFactory(_ => _root.Resolve(request.ServiceType, IfUnresolved.ReturnDefault)));

            var container = new Container(rules);
            container.Populate(serviceDescriptors);
            var resolver = new FunctionsResolver(container);

            var previous = Interlocked.Exchange(ref _currentResolver, resolver);

            if (!previous.IsRootContainer)
            {
                previous.Dispose();
            }
        }

        public IServiceScope CreateScope()
        {
            return _currentResolver.CreateChildScope();
        }

        public void Dispose()
        {
            _currentResolver.Dispose();
            if (!_root.IsDisposed)
            {
                _root.Dispose();
            }
        }
    }
}
