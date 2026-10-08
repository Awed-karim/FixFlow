using FixFlow.Application.Categories;
using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Entities;
using FixFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        return await _db.ServiceCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
            .ToListAsync();
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(int id)
    {
        var category = await _db.ServiceCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return Result<CategoryDto>.Fail("Category not found.", ErrorType.NotFound);

        return Result<CategoryDto>.Ok(ToDto(category));
    }

    public async Task<Result<CategoryDto>> CreateAsync(CategoryRequest request)
    {
        var name = request.Name.Trim();

        if (await _db.ServiceCategories.AnyAsync(c => c.Name == name))
            return Result<CategoryDto>.Fail("A category with this name already exists.", ErrorType.Conflict);

        var category = new ServiceCategory
        {
            Name = name,
            Description = request.Description?.Trim()
        };

        _db.ServiceCategories.Add(category);
        await _db.SaveChangesAsync();

        return Result<CategoryDto>.Ok(ToDto(category));
    }

    public async Task<Result<CategoryDto>> UpdateAsync(int id, CategoryRequest request)
    {
        var category = await _db.ServiceCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return Result<CategoryDto>.Fail("Category not found.", ErrorType.NotFound);

        var name = request.Name.Trim();

        if (await _db.ServiceCategories.AnyAsync(c => c.Name == name && c.Id != id))
            return Result<CategoryDto>.Fail("A category with this name already exists.", ErrorType.Conflict);

        category.Name = name;
        category.Description = request.Description?.Trim();
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Result<CategoryDto>.Ok(ToDto(category));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var category = await _db.ServiceCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return Result.Fail("Category not found.", ErrorType.NotFound);

        var inUse = await _db.TechnicianProfiles.AnyAsync(t => t.ServiceCategoryId == id)
                    || await _db.ServiceRequests.AnyAsync(r => r.ServiceCategoryId == id);

        if (inUse)
            return Result.Fail("Category is used by technicians or requests and cannot be deleted.", ErrorType.Conflict);

        _db.ServiceCategories.Remove(category);
        await _db.SaveChangesAsync();

        return Result.Ok();
    }

    private static CategoryDto ToDto(ServiceCategory c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description
    };
}