using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Persistence.Context;
using StaffSync.Persistence.Repositories;

namespace StaffSync.Persistence.UnitOfWorks
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly StaffSyncContext _context;

        public UnitOfWork(StaffSyncContext context)
        {
            _context = context;
        }

        public async ValueTask DisposeAsync() => await _context.DisposeAsync();

        public int Save() => _context.SaveChanges();

        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();

        IReadRepository<T> IUnitOfWork.GetReadRepository<T>() => new ReadRepository<T>(_context);

        IWriteRepository<T> IUnitOfWork.GetWriteRepository<T>() => new WriteRepository<T>(_context);
    }
}
