using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebEntryPoint.Helpers
{
    public static class RegEx
    {
        public const string InvalidMessageIdChars = "[^0-9a-zA-Z-_ ]+";
        public const string isPostalCode    = @"^[0-9][0-9][0-9][0-9]\s*[A-Z][A-Z]\s*[0-9]+$";
        public const string PostalGetPostal = @"^[0-9][0-9][0-9][0-9]\s*[A-Z][A-Z]";
        public const string PostalGetHousenr = @"[0-9]+\s*$";
    }
}