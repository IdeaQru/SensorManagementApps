using Microsoft.EntityFrameworkCore;
using SensorMonitorDesktop.Models;

namespace SensorMonitorDesktop.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<SensorGroup> SensorGroups => Set<SensorGroup>();
        public DbSet<SensorType> SensorTypes => Set<SensorType>();
        public DbSet<Sensor> Sensors => Set<Sensor>();
        public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
        public DbSet<LogSession> LogSessions => Set<LogSession>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // File DB akan bernama sensor_monitor.db di folder output (bin/Debug, dll.)
                optionsBuilder.UseSqlite("Data Source=sensor_monitor.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Unique name untuk group dan type
            modelBuilder.Entity<SensorGroup>()
                .HasIndex(g => g.Name)
                .IsUnique();

            modelBuilder.Entity<SensorType>()
                .HasIndex(t => t.Name)
                .IsUnique();

            // Index untuk query log per sensor dan waktu
            modelBuilder.Entity<SensorReading>()
                .HasIndex(r => new { r.SensorId, r.Timestamp });

            modelBuilder.Entity<Sensor>()
                .HasIndex(s => s.GroupId);
        }
    }
}
