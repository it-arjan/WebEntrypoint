using WebEntryPoint.Helpers;

namespace WebEntryPoint.ServiceCall
{
    public interface IWebserviceFactory
    {
        IWebService Create(QServiceConfig serviceNr, ITokenCache manager);
    }
}