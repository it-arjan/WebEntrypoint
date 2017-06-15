namespace WebEntryPoint.ServiceCall
{
    public interface ITokenCache
    {
        string GetToken(string scope);
    }
}