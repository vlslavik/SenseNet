using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    public interface ISettingsManager
    {
        T GetValue<T>(string settingsName, string key, string contextPath, T defaultValue);
    }

    /// <summary>
    /// This class servers as a representation of the Settings class that resides on the ContentRepository level.
    /// </summary>
    internal class Settings
    {
        private static ISettingsManager _settingsManager;
        private static ISettingsManager SettingsManager
        {
            get
            {
                if (_settingsManager == null)
                {
                    var defType = typeof (DefaultSettingsManager);
                    var smType = TypeHandler.GetTypesByInterface(typeof (ISettingsManager)).FirstOrDefault(t => t.FullName != defType.FullName) ?? defType;

                    _settingsManager = Activator.CreateInstance(smType) as ISettingsManager;
                }

                return _settingsManager;
            }
        }

        private class DefaultSettingsManager : ISettingsManager
        {
            T ISettingsManager.GetValue<T>(string settingsName, string key, string contextPath, T defaultValue)
            {
                return default(T);
            }
        }

        public static T GetValue<T>(string settingsName, string key, string contextPath,T defaultValue)
        {
            return SettingsManager.GetValue(settingsName, key, contextPath, defaultValue);
        }
    }
}
