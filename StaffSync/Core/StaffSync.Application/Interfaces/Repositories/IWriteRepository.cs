using StaffSync.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Interfaces.Repositories
{
    public interface IWriteRepository<T> where T : class, IEntityBase, new()
    {
        Task AddAysnc(T entity);

        Task AddRangeAsync(IList<T> entities);

        Task<T> UpdateAsync(T entity);

        Task HardDeleteAsync(T entity);
    }
}
