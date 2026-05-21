namespace FamilyTrip.Application.DTOs.Destination;

public class DestinationDetailsResponse
{
    public DestinationDto Destination { get; set; } = new();
    public WeatherSnapshotDto? Weather { get; set; }
    public List<PointOfInterestDto> Attractions { get; set; } = new();
    public HotelAvailabilityDto? Hotels { get; set; }
}

public class WeatherSnapshotDto
{
    public double AverageTempCelsius { get; set; }
    public double MinTempCelsius { get; set; }
    public double MaxTempCelsius { get; set; }
    public int RainyDays { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class PointOfInterestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public double? Rating { get; set; }
    public int? UserRatingsTotal { get; set; }
    public string? Address { get; set; }
}

public class HotelAvailabilityDto
{
    public int HotelsFound { get; set; }
    public List<HotelSampleDto> SampleHotels { get; set; } = new();
}

public class HotelSampleDto
{
    public string Name { get; set; } = string.Empty;
    public double? Rating { get; set; }
    public string? Address { get; set; }
}
