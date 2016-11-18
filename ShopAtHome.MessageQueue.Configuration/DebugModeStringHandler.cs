using System;
using System.Configuration;

namespace ShopAtHome.MessageQueue.Configuration
{
    public class DebugModeStringHandler
    {
        private const string _DEBUG_MODE_APP_SETTING_KEY = "DebugMode";
        private const string _DEBUG_MODE_APP_SETTING_VALUE = "true";

        private static bool ConfiguredDebugMode => ConfigurationManager.AppSettings.Get(_DEBUG_MODE_APP_SETTING_KEY) == _DEBUG_MODE_APP_SETTING_VALUE;

        /// <summary>
        /// Makes a machine-name specific value using configuration settings
        /// </summary>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        public static string MakeDebugIdentifierValue(string baseValue)
        {
            return MakeDebugIdentifierValue(baseValue, ConfiguredDebugMode);
        }

        /// <summary>
        /// Un-makes a machine-name specific value using configuration settings
        /// </summary>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        public static string UnmakeDebugIdentifierValue(string baseValue)
        {
            return UnmakeDebugIdentifierValue(baseValue, ConfiguredDebugMode);
        }

        public static string MakeDebugIdentifierValue(string baseValue, bool debugMode)
        {
            if (debugMode)
            {
                return string.IsNullOrWhiteSpace(baseValue) ? baseValue : $"{Environment.MachineName}_{baseValue}";
            }
            return baseValue;
        }

        public static string UnmakeDebugIdentifierValue(string baseValue, bool debugMode)
        {
            if (debugMode)
            {
                return string.IsNullOrWhiteSpace(baseValue) ? baseValue : baseValue.Replace($"{Environment.MachineName}_", string.Empty);
            }
            return baseValue;
        }
    }
}
