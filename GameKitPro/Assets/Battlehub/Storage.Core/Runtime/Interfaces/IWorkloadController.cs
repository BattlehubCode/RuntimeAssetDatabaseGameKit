using System.Threading.Tasks;

namespace Battlehub.Storage
{
    public interface IWorkloadController
    {
        ValueTask TryPostponeTask();
    }
}

