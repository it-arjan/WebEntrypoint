namespace WebEntryPoint.WebSockets
{
    public interface ISocketClient
    {
        void Send(string accessToken, string feedId, string msg);
    }
}