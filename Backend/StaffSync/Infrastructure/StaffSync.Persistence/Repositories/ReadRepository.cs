using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Domain.Common;
using StaffSync.Persistence.Context;
using System.Linq.Expressions;

namespace StaffSync.Persistence.Repositories
{
    public class ReadRepository<T> : IReadRepository<T> where T : class,IEntityBase, new()
    {
        private readonly StaffSyncContext _context;

        public ReadRepository(StaffSyncContext context)
        {
            _context = context;
        }
        private DbSet<T> Table { get=>_context.Set<T>(); }
       
        public async Task<IList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderby = null, bool enableTracking = false)
        {
            IQueryable<T> queryable = Table;
            if (!enableTracking) queryable = queryable.AsNoTracking(); // performans için sadece veriyi çekiyoruz
            if (include is not null) queryable = include(queryable);
            if(predicate is not null) queryable=queryable.Where(predicate);
            if(orderby is not null)  return await orderby(queryable).ToListAsync();

            return await queryable.ToListAsync();
        }
        public async Task<IList<T>> GetAllByPagingAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderby = null, bool enableTracking = false, int currentPage = 1, int pageSize = 3)
        {
            IQueryable<T> queryable = Table;
            if (!enableTracking) queryable = queryable.AsNoTracking(); // performans için sadece veriyi çekiyoruz
            if (include is not null) queryable = include(queryable);
            if (predicate is not null) queryable = queryable.Where(predicate);
            if (orderby is not null) return await orderby(queryable).Skip((currentPage-1)*pageSize).Take(pageSize).ToListAsync();

            return await queryable.ToListAsync();
        }
        public async Task<T> GetAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool enableTracking = false)
        {
            IQueryable<T> queryable = Table;
            if (!enableTracking) queryable = queryable.AsNoTracking(); // performans için sadece veriyi çekiyoruz
            if (include is not null) queryable = include(queryable);
            
            //queryable.Where(predicate);

            return await queryable.FirstOrDefaultAsync(predicate);
        }
        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            Table.AsNoTracking();
            if(predicate is not null) Table.Where(predicate);

            return await Table.CountAsync();
        }

        public IQueryable<T> Find(Expression<Func<T, bool>> predicate, bool enableTracking = false)
        {
            if(!enableTracking) Table.AsNoTracking();

            return Table.Where(predicate);
        }
    }
}
