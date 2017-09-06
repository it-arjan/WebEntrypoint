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
        static ILogger _logger = LogManager.CreateLogger(typeof(SettingsChecker));
        public static void CheckPlainSettings(Type settingsClass, List<string> excludeList)
        {

            List<FieldInfo> constStringFields = GetConstStringFields(settingsClass, excludeList);

            try
            {
                constStringFields.ForEach(fi => CheckSetting(fi.GetValue(null).ToString()));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        private static List<FieldInfo> GetConstStringFields(Type settingsClass, List<string> excludeList)
        {
            var constStrFields = settingsClass.GetFields().Where(
                            fi => fi.IsLiteral && !fi.IsInitOnly
                            && fi.FieldType == typeof(string)
                            && !fi.GetValue(null).ToString().Contains("@")).ToList();

            List<FieldInfo> result = new List<FieldInfo>();
            foreach (var f in constStrFields)
            {
                if (!excludeList.Contains(f.GetValue(null).ToString()))
                    result.Add(f);
            }
            return result;
        }
        private static void CheckSetting(string key)
        {
            if (ConfigurationManager.AppSettings.Get(key) == null)
                throw new Exception("Setting not present! =>" + key);
        }

        public static void CheckPresenceEnumeratedSettings(Type settingsClass, Type enumType, Enum enumval)
        {
            Debug.Assert(enumType.IsEnum);
            // example
            // key = "service@QServiceConfig@.max.load", 
            // QServiceConfig is an enum
            // settings checed: "service1.max.load, service1.name, service1.*"

            var enumSettingIndicator = string.Format("@{0}@", enumType.Name);

            List<FieldInfo> enumStringFields = GetConstStringEnumeratedFields(settingsClass, enumSettingIndicator);

            if (!enumStringFields.Any())
            {
                _logger.Warn(@"CheckPresenceEnumeratedSettings is checking presence of enumerated settings for enum {0}, 
but no settings containing placeholder {1} exists in settingsclass {2}",
                    enumType.Name, enumSettingIndicator, settingsClass.FullName);
            }
            try
            {
                enumStringFields.ForEach(fi => CheckSetting(
                    fi.GetValue(null).ToString()
                    .Replace(enumSettingIndicator, Convert.ToInt16(enumval).ToString()))
                    );
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        private static List<FieldInfo> GetConstStringEnumeratedFields(Type settingsClass, string enumSettingIndicator)
        {
            return settingsClass.GetFields()
                .Where(fi =>
                        fi.GetValue(null).ToString().Contains(enumSettingIndicator)
                        && fi.IsLiteral && !fi.IsInitOnly
                        && fi.FieldType == typeof(string)
                ).ToList();
        }
    }
}
