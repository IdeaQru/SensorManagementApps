using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SensorMonitorDesktop.Data;
using SensorMonitorDesktop.Models;

namespace SensorMonitorDesktop.Services
{
    public class SensorRepository
    {
        private readonly AppDbContext _db;

        public SensorRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<List<SensorGroup>> GetGroupsAsync()
            => _db.SensorGroups
                  .Include(g => g.Sensors)
                  .ToListAsync();

        public async Task AddGroupAsync(SensorGroup group)
        {
            _db.SensorGroups.Add(group);
            await _db.SaveChangesAsync();
        }

        public async Task AddSensorAsync(Sensor sensor)
        {
            _db.Sensors.Add(sensor);
            await _db.SaveChangesAsync();
        }

        // Tambah method lain sesuai kebutuhan (update, delete, logging, dsb.)
    }
}
