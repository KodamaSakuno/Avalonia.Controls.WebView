using Avalonia.Layout;

namespace Avalonia.Controls;

public class PrintOptions
{
    /// <summary>Page orientation</summary>
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>Page margins</summary>
    public Thickness Margins { get; set; } = new(0);

    /// <summary>Scaling factor</summary>
    public double Scale { get; set; } = 1.0;
}
