
using System.Linq.Expressions;
using RV2.Data.Postgres.Base.Model;

namespace RV2.Data.Postgres.Base.Repository
{
    public interface INpgsqlRepository
    {
        Task<List<T>> BatchDeactivateAsync<T>(Expression<Func<T, bool>> filter) where T : BaseData;
        Task<List<T>> BatchDeactivateAsync<T>(List<T> entities) where T : BaseData;
        Task<List<T>> BatchDeleteAsync<T>(List<T> entities) where T : BaseData;
        Task<List<T>> BatchInsertAsync<T>(List<T> entities, int batchSize = 500) where T : BaseData;
        Task<List<T>> BatchUpdateAsync<T>(List<T> entities) where T : BaseData;
        Task<bool> DeleteAsync<T>(T entity) where T : BaseData;
        IQueryable<T> Get<T>() where T : BaseData;        
        T Insert<T>(T entity) where T : BaseData;
        Task<T> InsertAsync<T>(T entity) where T : BaseData;
        T Update<T>(T entity) where T : BaseData;
        Task<T> UpdateAsync<T>(T entity) where T : BaseData;
    }
}