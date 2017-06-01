using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    public interface IWebService
    {
        int MaxLoad { get; }
        int MaxRetries { get; }
        string Name { get; set; }
        int ServiceLoad { get; set; }
        string Url { get; }
        int WaitingQueueLength { get; set; }

        Task<DataBag> Call(DataBag data);
        string Description();
        bool MaxLoadReached();
    }
}