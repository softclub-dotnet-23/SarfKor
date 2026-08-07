using Application.Abstractions;
using Application.Assistant;
using Application.Assistant.Abstractions;
using Application.Assistant.Tools;
using Application.Common;
using Application.Inventory.Queries.GetReorderAlerts;
using Application.Inventory.Queries.GetStockLevel;
using Application.Products.Queries.GetTopSellingProducts;
using Application.Sales.Queries.GetCashierAnomalyReport;
using Application.Sales.Queries.GetDailySalesReport;
using Application.Sales.Queries.GetProfitReport;
using Application.Stores.Queries.GetAllStores;
using Microsoft.Extensions.Options;
using Moq;

namespace Application.Tests.Assistant;

/// <summary>
/// The direct test of CLAUDE.md's hard requirement: "кассир спрашивает себестоимость — не
/// получает её ни прямо, ни косвенно". Exercises the *real* tool classes (not fakes) so a future
/// change to any tool's IsAvailableFor is caught here, not just in AssistantToolRegistryTests'
/// generic filtering logic.
/// </summary>
public class AssistantToolRoleGatingTests
{
    private static readonly AssistantCallerContext Cashier = new("cashier-1", 10, AssistantRole.Cashier);
    private static readonly AssistantCallerContext Owner = new("owner-1", 10, AssistantRole.StorePartner);
    private static readonly AssistantCallerContext Admin = new("admin-1", null, AssistantRole.Admin);

    private static IOptions<AssistantOptions> ActionsEnabled(bool enabled) =>
        Options.Create(new AssistantOptions { ActionsEnabled = enabled });

    [Fact]
    public void GetProfitReportTool_NeverAvailableToCashier()
    {
        var tool = new GetProfitReportTool(Mock.Of<IQueryHandler<GetProfitReportQuery, GetProfitReportResult>>());
        Assert.False(tool.IsAvailableFor(Cashier));
        Assert.True(tool.IsAvailableFor(Owner));
        Assert.False(tool.IsAvailableFor(Admin));
    }

    [Fact]
    public void GetDailySalesReportTool_NeverAvailableToCashier()
    {
        var tool = new GetDailySalesReportTool(Mock.Of<IQueryHandler<GetDailySalesReportQuery, GetDailySalesReportResult>>());
        Assert.False(tool.IsAvailableFor(Cashier));
        Assert.True(tool.IsAvailableFor(Owner));
    }

    [Fact]
    public void GetReorderAlertsTool_NeverAvailableToCashier()
    {
        var tool = new GetReorderAlertsTool(
            Mock.Of<IQueryHandler<GetReorderAlertsQuery, GetReorderAlertsResult>>(),
            Mock.Of<IProductRepository>());
        Assert.False(tool.IsAvailableFor(Cashier));
        Assert.True(tool.IsAvailableFor(Owner));
    }

    [Fact]
    public void GetCashierAnomalyReportTool_NeverAvailableToCashier()
    {
        var tool = new GetCashierAnomalyReportTool(Mock.Of<IQueryHandler<GetCashierAnomalyReportQuery, GetCashierAnomalyReportResult>>());
        Assert.False(tool.IsAvailableFor(Cashier));
        Assert.True(tool.IsAvailableFor(Owner));
    }

    [Fact]
    public void GetStockLevelTool_AvailableToBothCashierAndOwner_NotAdmin()
    {
        var tool = new GetStockLevelTool(
            Mock.Of<IQueryHandler<GetStockLevelQuery, GetStockLevelResult>>(),
            Mock.Of<IProductRepository>());
        Assert.True(tool.IsAvailableFor(Cashier));
        Assert.True(tool.IsAvailableFor(Owner));
        Assert.False(tool.IsAvailableFor(Admin));
    }

    [Fact]
    public void GetTopSellingProductsTool_AvailableToBothCashierAndOwner_NotAdmin()
    {
        var tool = new GetTopSellingProductsTool(Mock.Of<IQueryHandler<GetTopSellingProductsQuery, GetTopSellingProductsResult>>());
        Assert.True(tool.IsAvailableFor(Cashier));
        Assert.True(tool.IsAvailableFor(Owner));
        Assert.False(tool.IsAvailableFor(Admin));
    }

    [Fact]
    public void AdminTools_OnlyAvailableToAdmin()
    {
        var allStores = new GetAllStoresTool(Mock.Of<IQueryHandler<GetAllStoresQuery, GetAllStoresResult>>());

        foreach (var tool in new IAssistantTool[] { allStores })
        {
            Assert.True(tool.IsAvailableFor(Admin));
            Assert.False(tool.IsAvailableFor(Owner));
            Assert.False(tool.IsAvailableFor(Cashier));
        }
    }

    [Fact]
    public void ProposeCreatePromotionTool_OwnerOnly_AndOffByDefault()
    {
        var tool = new ProposeCreatePromotionTool(
            Mock.Of<IProductRepository>(),
            Mock.Of<IPendingAssistantActionRepository>(),
            Mock.Of<IUnitOfWork>(),
            ActionsEnabled(true));
        var toolDisabled = new ProposeCreatePromotionTool(
            Mock.Of<IProductRepository>(),
            Mock.Of<IPendingAssistantActionRepository>(),
            Mock.Of<IUnitOfWork>(),
            ActionsEnabled(false));

        Assert.True(tool.IsAvailableFor(Owner));
        Assert.False(tool.IsAvailableFor(Cashier));
        Assert.False(toolDisabled.IsAvailableFor(Owner));
    }

    [Fact]
    public void ProposeSetPriceAndRecordStockReceipt_AvailableToCashierAndOwner_WhenEnabled()
    {
        var setPrice = new ProposeSetPriceTool(
            Mock.Of<IProductRepository>(), Mock.Of<IPendingAssistantActionRepository>(), Mock.Of<IUnitOfWork>(), ActionsEnabled(true));
        var receipt = new ProposeRecordStockReceiptTool(
            Mock.Of<IProductRepository>(), Mock.Of<IPendingAssistantActionRepository>(), Mock.Of<IUnitOfWork>(), ActionsEnabled(true));

        Assert.True(setPrice.IsAvailableFor(Cashier));
        Assert.True(setPrice.IsAvailableFor(Owner));
        Assert.True(receipt.IsAvailableFor(Cashier));
        Assert.True(receipt.IsAvailableFor(Owner));
    }
}
