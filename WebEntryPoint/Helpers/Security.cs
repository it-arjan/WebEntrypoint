using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace WebEntryPoint.Helpers
{
    static class Security
    {
        public static X509Certificate2 GetCertificateFromStore(string name)
        {
            X509Certificate2 result = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                var certCollection = store.Certificates;

                foreach (var cert in certCollection)
                {
                    //Console.WriteLine("cert name: {0}", cert.Subject);
                    if (cert.Subject.Contains(name))
                    {
                        result = cert;
                        break;
                    }
                }
            }
            finally
            {
                store.Close();
            }
            if (result == null) throw new Exception("Unable to get certificate from store containing " + name);
            return result;
        }
        
    }
}
