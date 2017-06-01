namespace WebEntryPoint.ServiceCall
{
    public interface ITokenManager
    {
        string GetToken(string scope);
    }
}