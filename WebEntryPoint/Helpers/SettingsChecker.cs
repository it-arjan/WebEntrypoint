using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Reflection;
using NLogWrapper;
using System.Diagnostics;

namespace WebEntryPoint.Helpers
{
    public static class SettingsChecker
    {
        public static void CheckPresenceAllPlainSettings(Type settingsClass)
        {
            ILogger _logger = LogManager.CreateLogger(settingsClass);
            List<FieldInfo> constStringFields = settingsClass.GetFields().Where(
                fi => fi.IsLiteral && !fi.IsInitOnly 
                && fi.FieldType == typeof(string)
                && !fi.GetValue(null).ToString().Contains("@")).ToList();
            try
            {
                constStringFields.ForEach(fi => CheckPresencePlain(fi));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        private static void CheckPresencePlain(FieldInfo field)
        {
            string key = field.GetValue(null).ToString();
            if (ConfigurationManager.AppSettings.Get(key) == null)
                throw new Exception("Setting not present! =>" + key);
        }

        public static void CheckPresenceAllSettingsForThisEnumval(Type settingsClass, Type enumType, Enum enumval)
        {
            Debug.Assert(enumType.IsEnum);
            ILogger _logger = LogManager.CreateLogger(settingsClass);

            var toReplaceInKeyName = string.Format("@{0}@", enumType.Name);

            List<FieldInfo> enumStringFields = settingsClass.GetFields()
                .Where(fi => fi.GetValue(null).ToString().Contains(toReplaceInKeyName)
                        && fi.IsLiteral && !fi.IsInitOnly
                        && fi.FieldType == typeof(string)
                ).ToList();

            if (!enumStringFields.Any())
            {
                _logger.Warn("Code is checking presence of enum settings for enum {0}, but no fields like {1} exists in settingsclass {2}",
                    enumType.Name, toReplaceInKeyName, settingsClass.FullName);
            }
            try
            {
                enumStringFields.ForEach(fi => CheckPresenceEnum(fi, toReplaceInKeyName, enumval));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }
        private static void CheckPresenceEnum(FieldInfo field, string toReplace, Enum enumVal)
        {
            string key = field.GetValue(null).ToString()
                .Replace(toReplace, Convert.ToInt16(enumVal).ToString());

            if (ConfigurationManager.AppSettings.Get(key) == null)
                throw new Exception("Enum Setting not present! =>" + key);
        }
    }
}
