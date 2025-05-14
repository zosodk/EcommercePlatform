using System;

namespace EcommercePlatform.Domain.Entities;

public class GeoLocation // Owned type, typically for ListingItem
{
    // For GeoJSON compatibility, which MongoDB uses for geospatial queries
    //I really have to look into this more, but it seems like it's a bit of a pain to use - dammit'
    public string Type { get; private set; } = "Point";
    public double[] Coordinates { get; private set; } = Array.Empty<double>(); // [Longitude, Latitude]

    private GeoLocation() { } // For EF Core

    public static GeoLocation Create(double longitude, double latitude)
    {
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");

        return new GeoLocation
        {
            Coordinates = new double[] { longitude, latitude }
        };
    }

    public double Longitude => Coordinates.Length > 0 ? Coordinates[0] : 0;
    public double Latitude => Coordinates.Length > 1 ? Coordinates[1] : 0;
}