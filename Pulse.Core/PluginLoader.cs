using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Pulse.Core
{
    public static class PluginLoader
    {
        public static List<IModule> LoadPlugins(string pluginsDirectory, ActionEngine actionEngine, ConfigManager configManager)
        {
            var modules = new List<IModule>();
            if (!Directory.Exists(pluginsDirectory))
            {
                Directory.CreateDirectory(pluginsDirectory);
                return modules;
            }

            var dllFiles = Directory.GetFiles(pluginsDirectory, "Pulse.Plugins*.dll");
            foreach (var file in dllFiles)
            {
                try
                {
                    // To avoid locking, we could load bytes, but LoadFrom is fine for a monitoring app plugin dir.
                    var assembly = Assembly.LoadFrom(file);
                    var moduleTypes = assembly.GetTypes()
                        .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in moduleTypes)
                    {
                        IModule instance = null;
                        
                        var fullCtor = type.GetConstructor(new[] { typeof(ActionEngine), typeof(ConfigManager) });
                        if (fullCtor != null)
                        {
                            instance = (IModule)Activator.CreateInstance(type, actionEngine, configManager);
                        }
                        else
                        {
                            var partialCtor = type.GetConstructor(new[] { typeof(ActionEngine) });
                            if (partialCtor != null)
                            {
                                instance = (IModule)Activator.CreateInstance(type, actionEngine);
                            }
                            else
                            {
                                instance = (IModule)Activator.CreateInstance(type);
                            }
                        }

                        if (instance != null)
                        {
                            modules.Add(instance);
                            Logger.Info($"Loaded plugin module: {instance.Name} from {Path.GetFileName(file)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load plugin {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            return modules;
        }
    }
}
