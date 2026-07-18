using Application.Abstractions;
using Domain.Loyalty;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class LoyaltyProgramRepository(AppDbContext dbContext) : ILoyaltyProgramRepository
{
    public Task<LoyaltyProgram?> GetByIdAsync(int loyaltyProgramId, CancellationToken cancellationToken) =>
        dbContext.LoyaltyPrograms.FirstOrDefaultAsync(p => p.Id == loyaltyProgramId, cancellationToken);

    public Task<LoyaltyProgram?> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken) =>
        dbContext.LoyaltyPrograms.FirstOrDefaultAsync(p => p.StoreId == storeId, cancellationToken);

    public void Add(LoyaltyProgram loyaltyProgram) => dbContext.LoyaltyPrograms.Add(loyaltyProgram);
}
