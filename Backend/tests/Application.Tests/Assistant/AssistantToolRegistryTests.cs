using Application.Assistant;
using Application.Assistant.Abstractions;
using Application.Assistant.Tools;

namespace Application.Tests.Assistant;

/// <summary>A minimal double, not a real tool -- exists so registry filtering can be tested in
/// isolation from any real tool's business logic.</summary>
file sealed class FakeTool(string name, Func<AssistantCallerContext, bool> availableFor) : IAssistantTool
{
    public string Name => name;
    public string Description => "fake";
    public string InputSchemaJson => """{"type":"object","properties":{},"required":[]}""";
    public bool IsAvailableFor(AssistantCallerContext context) => availableFor(context);
    public Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken) =>
        Task.FromResult(new AssistantToolExecutionResult("ok"));
}

public class AssistantToolRegistryTests
{
    private static readonly AssistantCallerContext CashierContext = new("cashier-1", 10, AssistantRole.Cashier);
    private static readonly AssistantCallerContext OwnerContext = new("owner-1", 10, AssistantRole.StorePartner);

    [Fact]
    public void GetToolsFor_OnlyReturnsToolsAvailableForThatRole()
    {
        var ownerOnlyTool = new FakeTool("owner_only", ctx => ctx.Role == AssistantRole.StorePartner);
        var sharedTool = new FakeTool("shared", _ => true);
        var registry = new AssistantToolRegistry([ownerOnlyTool, sharedTool]);

        var cashierTools = registry.GetToolsFor(CashierContext);
        var ownerTools = registry.GetToolsFor(OwnerContext);

        Assert.DoesNotContain(cashierTools, t => t.Name == "owner_only");
        Assert.Contains(cashierTools, t => t.Name == "shared");
        Assert.Contains(ownerTools, t => t.Name == "owner_only");
    }

    // The direct test of the security requirement: even if a model somehow asks for a tool by name
    // that a Cashier was never offered, FindAvailable must not hand it back for execution.
    [Fact]
    public void FindAvailable_ToolNameOutsideCallersFilteredSet_ReturnsNull()
    {
        var ownerOnlyTool = new FakeTool("get_profit_report", ctx => ctx.Role == AssistantRole.StorePartner);
        var registry = new AssistantToolRegistry([ownerOnlyTool]);

        var found = registry.FindAvailable("get_profit_report", CashierContext);

        Assert.Null(found);
    }

    [Fact]
    public void FindAvailable_ToolNameWithinCallersFilteredSet_ReturnsTheTool()
    {
        var sharedTool = new FakeTool("get_stock_levels", _ => true);
        var registry = new AssistantToolRegistry([sharedTool]);

        var found = registry.FindAvailable("get_stock_levels", CashierContext);

        Assert.NotNull(found);
        Assert.Equal("get_stock_levels", found!.Name);
    }
}
