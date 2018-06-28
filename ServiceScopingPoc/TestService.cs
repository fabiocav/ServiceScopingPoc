using System;

namespace ServiceScopingPoc
{
    public class TestService : IServiceA, IServiceB, IDisposable
    {
        public TestService()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; set; }

        public void Dispose()
        {
            
        }
    }

    public interface IServiceA
    {
        string Id { get; }
    }

    public interface IServiceB
    {
        string Id { get; }
    }
}