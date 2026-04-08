using Microsoft.Maui.Devices.Sensors;

namespace TravelApp.Models.Runtime;

public sealed class RouteGeometryResult
{
    public IReadOnlyList<RouteGeometrySegment> Segments { get; set; } = [];

    public IReadOnlyList<Location> FlattenedPoints => Segments.SelectMany(x => x.Points).ToList();
}

public sealed class RouteGeometrySegment
{
    public int Index { get; set; }
    public string Label { get; set; } = string.Empty;
    public IReadOnlyList<Location> Points { get; set; } = [];
}
