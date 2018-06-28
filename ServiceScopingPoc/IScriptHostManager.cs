using System.Threading;
using System.Threading.Tasks;

namespace ServiceScopingPoc
{
    public interface IScriptHostManager
    {
        Task RestartHostAsync(CancellationToken cancellationToken);
    }
}