using System.Net;
using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    public interface IWebService
    {
        int MaxLoad { get; }
        int MaxRetries { get; }
        string Name { get; set; }
        int ServiceLoad { get; set; }
        string Url { get; set; }
        int WaitingQueueLength { get; set; }

        Task<DataBag> CallAsync(DataBag data);
        HttpStatusCode CallSync(DataBag data);
        string Description();
        bool MaxLoadReached();
    }
}