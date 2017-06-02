namespace WebEntryPoint.WebSockets
{
    public interface ISocketServer
    {
        void Start(string url);
        void WireFleckLogging();
    }
}