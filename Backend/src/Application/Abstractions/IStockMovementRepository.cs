using Domain.Inventory;

namespace Application.Abstractions;

public interface IStockMovementRepository
{
    void Add(StockMovement stockMovement);
}
