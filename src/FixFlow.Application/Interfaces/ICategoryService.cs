using FixFlow.Application.Categories;
using FixFlow.Application.Common;

namespace FixFlow.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<Result<CategoryDto>> GetByIdAsync(int id);
    Task<Result<CategoryDto>> CreateAsync(CategoryRequest request);
    Task<Result<CategoryDto>> UpdateAsync(int id, CategoryRequest request);
    Task<Result> DeleteAsync(int id);
}