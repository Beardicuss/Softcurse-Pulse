namespace Pulse.Core
{
    public interface IModule
    {
        string Name { get; }
        bool IsEnabled { get; set; }
        Task StartAsync();
        Task StopAsync();
    }
}
