namespace WebEntryPoint.WebSockets
{
    public interface ISocketClient
    {
        void Send(string sessionToken, string msg, params object[] msgPars);
    }
}