using FixFlow.Application.Common;
using Xunit;

namespace FixFlow.Tests;

public class GeoMathTests
{
    [Fact]
    public void Distance_between_same_point_is_zero()
    {
        var d = GeoMath.DistanceKm(29.3084, 30.8428, 29.3084, 30.8428);
        Assert.Equal(0, d, 3);
    }

    [Fact]
    public void Distance_between_Cairo_and_Fayoum_is_about_90_km()
    {
        var d = GeoMath.DistanceKm(30.0444, 31.2357, 29.3084, 30.8428);
        Assert.InRange(d, 85, 95);
    }

    [Fact]
    public void Distance_is_symmetric()
    {
        var a = GeoMath.DistanceKm(29.31, 30.84, 29.40, 30.90);
        var b = GeoMath.DistanceKm(29.40, 30.90, 29.31, 30.84);
        Assert.Equal(a, b, 6);
    }
}