namespace CSE325_visioncoders.Models;

using System;

public class OrderViewSettings
{
    // Inicio de día operativo local (por defecto 08:00)
    public TimeOnly DayStart { get; set; } = new(8, 0);

    public TimeOnly NoonSwitch { get; set; } = new(12, 0);

    // Zona horaria. Por defecto usa la local del servidor/PC
    public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;
}