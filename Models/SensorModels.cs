using System;
using System.Collections.Generic;

namespace SensorMonitorDesktop.Models
{
    public class SensorGroup
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
    }

    public class SensorType
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string DataType { get; set; } = "float";  // ✅ Default value
        public string? Unit { get; set; }
        public string? Description { get; set; }

        public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
    }

    public class Sensor
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public SensorGroup Group { get; set; } = null!;

        public int SensorTypeId { get; set; }
        public SensorType SensorType { get; set; } = null!;

        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SensorReading> Readings { get; set; } = new List<SensorReading>();
    }

    public class SensorReading
    {
        public int Id { get; set; }

        public int SensorId { get; set; }
        public Sensor Sensor { get; set; } = null!;

        public DateTime Timestamp { get; set; }
        public string Value { get; set; } = null!;  // ✅ String untuk fleksibilitas
        public string? Status { get; set; }
    }

    public class LogSession
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public int? GroupId { get; set; }
        public SensorGroup? Group { get; set; }

        public DateTime RangeStart { get; set; }
        public DateTime RangeEnd { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
