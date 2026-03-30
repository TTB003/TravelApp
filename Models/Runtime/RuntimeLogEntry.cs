namespace TravelApp.Models.Runtime;

public sealed record RuntimeLogEntry(DateTimeOffset TimestampUtc, string Source, string Message);
