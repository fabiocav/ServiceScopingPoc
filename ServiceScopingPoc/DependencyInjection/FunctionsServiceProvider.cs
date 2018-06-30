using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceScopingPoc
{
    public class FunctionsServiceProvider : IServiceProvider, IServiceScopeFactory
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

            _currentResolver = new FunctionsResolver(_root);
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

            var resolver = new Container(rules);
            resolver.Populate(serviceDescriptors);

            var previous = _currentResolver;
            _currentResolver = new FunctionsResolver(resolver);

            if (!ReferenceEquals(previous.Container, _root))
            {
                previous.Dispose();
            }
        }

        public IServiceScope CreateScope()
        {
            return _currentResolver.CreateChildScope();
        }
    }

    internal class FunctionsResolver : IDisposable
    {
        public FunctionsResolver(IContainer resolver)
        {
            Container = resolver;
            ChildScopes = new HashSet<FunctionsServiceScope>();
        }

        public IContainer Container { get; }

        public HashSet<FunctionsServiceScope> ChildScopes { get; }

        public void Dispose()
        {
            Task childScopeTasks = Task.WhenAll(ChildScopes.Select(s => s.DisposalTask));
            Task.WhenAny(childScopeTasks, Task.Delay(5000))
                .ContinueWith(t =>
                {
                    Container.Dispose();
                });
        }

        internal FunctionsServiceScope CreateChildScope()
        {
            IResolverContext scopedContext = Container.OpenScope();
            var scope = new FunctionsServiceScope(scopedContext);
            ChildScopes.Add(scope);

            scope.DisposalTask.ContinueWith(t => ChildScopes.Remove(scope));

            return scope;
        }
    }

    public class FunctionsServiceScope : IServiceScope
    {
        private readonly TaskCompletionSource<object> _activeTcs;
        private readonly ScopedServiceProvider _serviceProvider;

        public FunctionsServiceScope(IResolverContext serviceProvider)
        {
            _activeTcs = new TaskCompletionSource<object>();
            _serviceProvider = new ScopedServiceProvider(serviceProvider);
        }

        public IServiceProvider ServiceProvider => _serviceProvider;

        public Task DisposalTask => _activeTcs.Task;

        public void Dispose()
        {
            _serviceProvider.Dispose();
            _activeTcs.SetResult(null);
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
