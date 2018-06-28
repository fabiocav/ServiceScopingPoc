using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceScopingPoc
{
    public class FunctionsServiceProvider : IServiceProvider, IServiceScopeFactory
    {
        private IServiceProvider _inner;
        private IServiceCollection _child;
        private readonly IServiceCollection _rootDescriptors;

        public FunctionsServiceProvider(IServiceCollection descriptors)
        {
            _rootDescriptors = descriptors;
        }

        public string State { get; set; }

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

            var service =  _inner.GetService(serviceType);
            string name = serviceType.Name;

            return service;
        }

        public void AddServices(IServiceCollection services)
        {
            foreach (var item in services)
            {
                _rootDescriptors.Add(item);
            }
        }

        public void Build()
        {
            var collection = new ServiceCollection();
            IEnumerable<ServiceDescriptor> rootDescriptors = _rootDescriptors;

            if (_child != null)
            {
                AddServices(collection, _child);
                rootDescriptors = _rootDescriptors.Where(d => d.ServiceType != typeof(IHostedService));
            }

            AddServices(collection, rootDescriptors);

            collection.AddSingleton<FunctionsServiceProvider>(this);
            var sp = collection.FirstOrDefault(d => d.ServiceType == typeof(IServiceProvider));

            collection.AddSingleton<IServiceProvider>(this);
            _inner = collection.BuildServiceProvider();
        }

        internal void UpdateChildServices(IServiceCollection serviceDescriptors)
        {
            _child = serviceDescriptors;
        }

        private void AddServices(ICollection<ServiceDescriptor> collection, IEnumerable<ServiceDescriptor> descriptors)
        {
            foreach (var item in descriptors)
            {
                collection.Add(item);
            }
        }

        public IServiceScope CreateScope()
        {
            return _inner.CreateScope();
        }
    }
}
