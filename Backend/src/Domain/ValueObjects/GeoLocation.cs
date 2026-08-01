namespace Domain.ValueObjects;

public sealed record GeoLocation
{
    private readonly double _latitude;
    private readonly double _longitude;

    public double Latitude
    {
        get => _latitude;
        init => _latitude = value is >= -90 and <= 90
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Latitude), value, "Latitude must be between -90 and 90.");
    }

    public double Longitude
    {
        get => _longitude;
        init => _longitude = value is >= -180 and <= 180
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Longitude), value, "Longitude must be between -180 and 180.");
    }

    public GeoLocation(double Latitude, double Longitude)
    {
        this.Latitude = Latitude;
        this.Longitude = Longitude;
    }
}
