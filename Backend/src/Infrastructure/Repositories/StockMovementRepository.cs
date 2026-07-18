using Application.Abstractions;
using Domain.Inventory;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class StockMovementRepository(AppDbContext dbContext) : IStockMovementRepository
{
    public void Add(StockMovement stockMovement) => dbContext.StockMovements.Add(stockMovement);
}
