using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Domain.Common;
using StaffSync.Persistence.Context;

namespace StaffSync.Persistence.Repositories
{
    public class WriteRepository<T> : IWriteRepository<T> where T : class, IEntityBase, new()
    {
        private readonly StaffSyncContext _context;

        public WriteRepository(StaffSyncContext context)
        {
            _context = context;
        }
        private DbSet<T> Table { get => _context.Set<T>(); }

        public async Task AddAysnc(T entity)
        {
            await Table.AddAsync(entity);
        }

        public async Task AddRangeAsync(IList<T> entities)
        {
            await Table.AddRangeAsync(entities);
        }
        public async Task<T> UpdateAsync(T entity)
        {
            await Task.Run (()=>Table.Update(entity));
            return entity;
        }
        public async Task HardDeleteAsync(T entity)
        {
            await Task.Run(() => Table.Remove(entity));
        }
    }
}
