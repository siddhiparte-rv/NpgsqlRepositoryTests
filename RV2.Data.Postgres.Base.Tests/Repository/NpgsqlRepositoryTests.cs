using Microsoft.EntityFrameworkCore;
using RV2.Data.Postgres.Base.Repository;
using RV2.Data.Postgres.Base.Tests.Db;
using RV2.Data.Postgres.Base.Tests.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

namespace RV2.Data.Postgres.Base.Tests.Repository
{
    public class NpgsqlRepositoryTests : IAsyncLifetime
    {
        private DockerCompose _dockerCompose;
        private TestDbContext _dbContext;
        private NpgsqlRepository _repository;

        public async Task InitializeAsync()
        {
            _dockerCompose = new DockerCompose();
            await _dockerCompose.StartPostgresContainerAsync();

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseNpgsql(_dockerCompose.ConnectionString)
                .Options;

            _dbContext = new TestDbContext(options);
            await _dbContext.Database.EnsureCreatedAsync();

            _repository = new NpgsqlRepository(_dbContext);
        }

        public async Task DisposeAsync()
        {
            if (_dbContext != null)
            {
                await _dbContext.Database.EnsureDeletedAsync();
                await _dbContext.DisposeAsync();
            }

            if (_dockerCompose != null)
            {
                await _dockerCompose.DisposeAsync();
            }
        }

        [Fact]
        public async Task InsertAsync_ShouldInsertEntity()
        {
            // Arrange
            var entity = new TestEntity
            {
                Name = "Test",
                Description = "Description",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = "testuser",
                IsActive = true
            };

            // Act
            var result = await _repository.InsertAsync(entity);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);

            var dbEntity = await _dbContext.TestEntities.FindAsync(result.Id);
            Assert.NotNull(dbEntity);
            Assert.Equal("Test", dbEntity.Name);
        }

        [Fact]
        public void Insert_ShouldInsertEntity()
        {
            // Arrange
            var entity = new TestEntity
            {
                Name = "Test Sync",
                Description = "Description Sync",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = "testuser",
                IsActive = true
            };

            // Act
            var result = _repository.Insert(entity);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);

            _dbContext.ChangeTracker.Clear();

            var dbEntity = _dbContext.TestEntities.Find(result.Id);
            Assert.NotNull(dbEntity);
            Assert.Equal("Test Sync", dbEntity.Name);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEntity()
        {
            // Arrange
            var original = new TestEntity
            {
                Name = "Original",
                Description = "Original Description",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = "testuser",
                IsActive = true
            };

            await _repository.InsertAsync(original);

            _dbContext.Entry(original).State = EntityState.Detached;

            original.Name = "Updated";
            original.Description = "Updated Description";

            // Act
            var result = await _repository.UpdateAsync(original);

            // Assert
            Assert.Equal("Updated", result.Name);

            _dbContext.ChangeTracker.Clear();
            var dbEntity = await _dbContext.TestEntities.FindAsync(original.Id);

            Assert.NotNull(dbEntity);
            Assert.Equal("Updated", dbEntity.Name);
            Assert.Equal("Updated Description", dbEntity.Description);
        }

        [Fact]
        public void Update_ShouldUpdateEntity()
        {
            // Arrange
            var original = new TestEntity
            {
                Name = "Original Sync",
                Description = "Original Description Sync",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = "testuser",
                IsActive = true
            };

            _repository.Insert(original);

            _dbContext.Entry(original).State = EntityState.Detached;

            original.Name = "Updated Sync";
            original.Description = "Updated Description Sync";

            // Act
            var result = _repository.Update(original);

            // Assert
            Assert.Equal("Updated Sync", result.Name);

            _dbContext.ChangeTracker.Clear();
            var dbEntity = _dbContext.TestEntities.Find(original.Id);

            Assert.NotNull(dbEntity);
            Assert.Equal("Updated Sync", dbEntity.Name);
            Assert.Equal("Updated Description Sync", dbEntity.Description);
        }

        [Fact]
        public async Task Get_ShouldReturnEntities()
        {
            // Arrange
            var entities = Enumerable.Range(1, 5)
                .Select(i => new TestEntity
                {
                    Name = $"Test {i}",
                    Description = $"Description {i}",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "testuser",
                    IsActive = true
                }).ToList();

            await _repository.BatchInsertAsync(entities);

            // Act
            var results = _repository.Get<TestEntity>().ToList();

            // Assert
            Assert.Equal(5, results.Count);
            Assert.Contains(results, e => e.Name.StartsWith("Test"));
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteEntity()
        {
            // Arrange
            var entity = new TestEntity
            {
                Name = "To Delete",
                Description = "Will be deleted",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = "testuser",
                IsActive = true
            };

            await _repository.InsertAsync(entity);

            _dbContext.Entry(entity).State = EntityState.Detached;

            // Act
            await _repository.DeleteAsync(entity);

            // Assert
            var dbEntity = await _dbContext.TestEntities.FindAsync(entity.Id);
            Assert.Null(dbEntity);
        }

        [Fact]
        public async Task BatchOperations()
        {
            // Arrange
            var entities = Enumerable.Range(1, 10)
                .Select(i => new TestEntity
                {
                    Name = $"Test {i}",
                    Description = $"Description {i}",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "testuser",
                    IsActive = true
                }).ToList();

            // Act - Batch Insert
            var inserted = await _repository.BatchInsertAsync(entities);

            // Assert
            Assert.Equal(10, inserted.Count);
            Assert.All(inserted, e => Assert.True(e.Id > 0));

            _dbContext.ChangeTracker.Clear();

            // Act - Batch Update
            foreach (var entity in inserted)
            {
                entity.Name += " Updated";
            }

            var updated = await _repository.BatchUpdateAsync(inserted);

            // Assert
            Assert.All(updated, e => Assert.EndsWith("Updated", e.Name));

            _dbContext.ChangeTracker.Clear();

            // Act - Batch Deactivate
            var deactivated = await _repository.BatchDeactivateAsync(updated);

            // Assert
            Assert.All(deactivated, e => Assert.False(e.IsActive));

            // Verify in database
            _dbContext.ChangeTracker.Clear();
            var dbEntities = await _dbContext.TestEntities.ToListAsync();
            Assert.All(dbEntities, e => Assert.False(e.IsActive));
        }

        [Fact]
        public async Task BatchDeactivateAsync_WithFilter_ShouldDeactivateMatchingEntities()
        {
            // Arrange
            var entities = Enumerable.Range(1, 10)
                .Select(i => new TestEntity
                {
                    Name = i <= 5 ? $"Group A {i}" : $"Group B {i}",
                    Description = $"Description {i}",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "testuser",
                    IsActive = true
                }).ToList();

            await _repository.BatchInsertAsync(entities);
            _dbContext.ChangeTracker.Clear();

            // Act - Deactivate only Group A entities
            var deactivated = await _repository.BatchDeactivateAsync<TestEntity>(e => e.Name.StartsWith("Group A"));

            // Assert
            Assert.Equal(5, deactivated.Count);
            Assert.All(deactivated, e => Assert.False(e.IsActive));
            Assert.All(deactivated, e => Assert.StartsWith("Group A", e.Name));

            // Verify that Group B entities are still active
            _dbContext.ChangeTracker.Clear();
            var groupBEntities = await _dbContext.TestEntities.Where(e => e.Name.StartsWith("Group B")).ToListAsync();
            Assert.All(groupBEntities, e => Assert.True(e.IsActive));
        }

        [Fact]
        public async Task BatchDeleteAsync_ShouldDeleteEntities()
        {
            // Arrange
            var entitiesToDelete = Enumerable.Range(1, 5)
                .Select(i => new TestEntity
                {
                    Name = $"Delete Me {i}",
                    Description = $"To Be Deleted {i}",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "testuser",
                    IsActive = true
                }).ToList();

            var entitiesToKeep = Enumerable.Range(6, 5)
                .Select(i => new TestEntity
                {
                    Name = $"Keep Me {i}",
                    Description = $"Should Remain {i}",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "testuser",
                    IsActive = true
                }).ToList();

            // Insert all entities
            await _repository.BatchInsertAsync(entitiesToDelete);
            await _repository.BatchInsertAsync(entitiesToKeep);

            // Verify we have 10 entities before deletion
            Assert.Equal(10, _dbContext.TestEntities.Count());

            // Clear tracker to ensure accurate state
            _dbContext.ChangeTracker.Clear();

            // Act
            var deleted = await _repository.BatchDeleteAsync(entitiesToDelete);

            // Assert
            Assert.Equal(5, deleted.Count);

            _dbContext.ChangeTracker.Clear();
            var remainingEntities = await _dbContext.TestEntities.ToListAsync();

            // Verify only 5 entities remain and they're the correct ones
            Assert.Equal(5, remainingEntities.Count);
            Assert.All(remainingEntities, e => Assert.StartsWith("Keep Me", e.Name));
            Assert.Equal(0, remainingEntities.Count(e => e.Name.StartsWith("Delete Me")));
        }
    }
}
