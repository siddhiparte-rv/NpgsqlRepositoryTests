using Microsoft.EntityFrameworkCore;
using RV2.Data.Postgres.Base.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using RV2.Util.Base;


namespace RV2.Data.Postgres.Base.Repository
{
    public class NpgsqlRepository: INpgsqlRepository
    {
        public NpgsqlContext _context;

        public NpgsqlRepository(NpgsqlContext context)
        {
            _context = context;
            _context.Database.SetCommandTimeout(2500);
        }

        public IQueryable<T> Get<T>() where T : BaseData
        {
            return _context.Set<T>().AsNoTracking();
        }

        public T Insert<T>(T entity) where T : BaseData
        {
            if (entity == null) throw new ArgumentNullException("entity");
            _context.Set<T>().Attach(entity);
            _context.Entry(entity).State = EntityState.Added;
            _context.SaveChanges();

            return entity;
        }

        public async Task<T> InsertAsync<T>(T entity) where T : BaseData
        {

            if (entity == null) throw new ArgumentNullException("entity");
            _context.Set<T>().Attach(entity);
            _context.Entry(entity).State = EntityState.Added;
            await _context.SaveChangesAsync();
            return entity;
        }

        public T Update<T>(T entity) where T : BaseData
        {
            if (entity == null) throw new ArgumentNullException("entity");

            _context.Set<T>().Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;

            _context.SaveChanges();
            return entity;
        }

        public async Task<T> UpdateAsync<T>(T entity) where T : BaseData
        {
            if (entity == null) throw new ArgumentNullException("entity");

            _context.ChangeTracker.Clear();
            _context.Set<T>().Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync<T>(T entity) where T : BaseData
        {
            _context.Set<T>().Attach(entity);
            _context.Entry(entity).State = EntityState.Deleted;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<T>> BatchInsertAsync<T>(List<T> entities, int batchSize = 500) where T : BaseData
        {
            if (entities == null) throw new ArgumentNullException("entities");

            var batchedEntities = entities.Batch(batchSize);

            foreach (var batch in batchedEntities)
            {
                _context.Set<T>().AddRange(batch);
                batch.Select(x => _context.Entry(x).State = EntityState.Added);

                await _context.SaveChangesAsync();
            }

            return entities;
        }

        public async Task<List<T>> BatchUpdateAsync<T>(List<T> entities) where T : BaseData
        {
            if (entities == null) throw new ArgumentNullException("entities");
            _context.ChangeTracker.Clear();
            _context.Set<T>().AddRange(entities);
            entities.ForEach(x =>
                _context.Entry(x).State = EntityState.Modified
            );
            await _context.SaveChangesAsync();
            return entities;
        }

        public async Task<List<T>> BatchDeleteAsync<T>(List<T> entities) where T : BaseData
        {
            if (entities == null) throw new ArgumentNullException("entities");

            foreach (var entity in entities)
                _context.Set<T>().Remove(entity);

            await _context.SaveChangesAsync();
            return entities;
        }

        public async Task<List<T>> BatchDeactivateAsync<T>(List<T> entities) where T : BaseData
        {
            if (entities == null) throw new ArgumentNullException("entities");
            _context.ChangeTracker.Clear();

            entities.ForEach(x =>x.IsActive = false);
            _context.Set<T>().AddRange(entities);
            entities.ForEach(x =>
                _context.Entry(x).State = EntityState.Modified
            );
            await _context.SaveChangesAsync();
            return entities;
        }

        public async Task<List<T>> BatchDeactivateAsync<T>(Expression<Func<T, bool>> filter) where T : BaseData
        {
            var entities = await Get<T>().Where(filter).ToListAsync();
            var deactivatedEntities = await BatchDeactivateAsync(entities);

            return deactivatedEntities;
        }
    }
}
