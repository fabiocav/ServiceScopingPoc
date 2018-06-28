using System;

namespace ServiceScopingPoc
{
    public class TestService : IServiceA, IServiceB
    {
        public TestService()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; set; }
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