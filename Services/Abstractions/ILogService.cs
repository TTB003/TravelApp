using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface ILogService
{
    bool IsEnabled { get; set; }

    event EventHandler<RuntimeLogEntry>? LogAdded;

    IReadOnlyList<RuntimeLogEntry> GetLogs();

    void Log(string source, string message);

    void Clear();
}
