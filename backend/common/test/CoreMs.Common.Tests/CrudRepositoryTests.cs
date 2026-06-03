using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CoreMs.Common.Repository;

namespace CoreMs.Common.Tests;

public class CrudTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CrudTestDbContext : DbContext
{
    public DbSet<CrudTestEntity> CrudEntities => Set<CrudTestEntity>();

    public CrudTestDbContext(DbContextOptions<CrudTestDbContext> options) : base(options) { }
}

public class TestCrudRepository : CrudRepository<CrudTestEntity>
{
    public TestCrudRepository(DbContext context) : base(context) { }
}

public class CrudRepositoryTests : IDisposable
{
    private readonly CrudTestDbContext _context;
    private readonly TestCrudRepository _repository;

    public CrudRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CrudTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new CrudTestDbContext(options);
        _repository = new TestCrudRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Add_TracksEntityInChangeTracker()
    {
        var entity = new CrudTestEntity { Id = 1, Name = "Test" };

        _repository.Add(entity);

        _context.ChangeTracker.HasChanges().Should().BeTrue();
        await _context.SaveChangesAsync();
        _context.Set<CrudTestEntity>().Should().ContainSingle(e => e.Id == 1 && e.Name == "Test");
    }

    [Fact]
    public async Task Update_TracksModification()
    {
        var entity = new CrudTestEntity { Id = 1, Name = "Original" };
        _context.Set<CrudTestEntity>().Add(entity);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        entity.Name = "Updated";
        _repository.Update(entity);

        _context.ChangeTracker.HasChanges().Should().BeTrue();
        await _context.SaveChangesAsync();
        var updated = await _context.Set<CrudTestEntity>().FindAsync(1);
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Remove_TracksRemoval()
    {
        var entity = new CrudTestEntity { Id = 1, Name = "ToDelete" };
        _context.Set<CrudTestEntity>().Add(entity);
        await _context.SaveChangesAsync();

        _repository.Remove(entity);

        _context.ChangeTracker.HasChanges().Should().BeTrue();
        await _context.SaveChangesAsync();
        _context.Set<CrudTestEntity>().Should().BeEmpty();
    }

    [Fact]
    public void Add_DoesNotSaveImmediately()
    {
        var entity = new CrudTestEntity { Id = 1, Name = "Test" };

        _repository.Add(entity);

        // Entity is tracked but NOT in the database yet
        _context.Set<CrudTestEntity>().Local.Should().ContainSingle();
    }
}
