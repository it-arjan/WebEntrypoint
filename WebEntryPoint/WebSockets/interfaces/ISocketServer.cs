namespace WebEntryPoint.WebSockets
{
    public interface ISocketServer
    {
        void Start();
        void WireFleckLogging();
        void CheckinToken(string accessToken);
    }
}