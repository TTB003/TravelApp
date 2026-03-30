using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public class RuntimeLogService : ILogService
{
    private const int MaxLogEntries = 500;

    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();
    private readonly List<RuntimeLogEntry> _entries = [];

    public bool IsEnabled { get; set; }

    public event EventHandler<RuntimeLogEntry>? LogAdded;

    public RuntimeLogService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<RuntimeLogEntry> GetLogs()
    {
        lock (_sync)
        {
            return _entries.ToArray();
        }
    }

    public void Log(string source, string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        RuntimeLogEntry entry;
        lock (_sync)
        {
            entry = new RuntimeLogEntry(_timeProvider.GetUtcNow(), source, message);
            _entries.Add(entry);

            if (_entries.Count > MaxLogEntries)
            {
                var removeCount = _entries.Count - MaxLogEntries;
                _entries.RemoveRange(0, removeCount);
            }
        }

        LogAdded?.Invoke(this, entry);
    }

    public void Clear()
    {
        lock (_sync)
        {
            _entries.Clear();
        }
    }
}
