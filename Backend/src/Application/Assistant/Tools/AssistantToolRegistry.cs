using Application.Assistant.Abstractions;

namespace Application.Assistant.Tools;

/// <summary>
/// The only place that decides which tools a given chat turn's model call is even offered --
/// registered <see cref="IAssistantTool"/> implementations are filtered by
/// <see cref="IAssistantTool.IsAvailableFor"/> per <see cref="AssistantCallerContext"/>, so a
/// Cashier's model call is never handed a profit/cost-price tool to begin with, and Mode C tools
/// disappear entirely whenever <see cref="AssistantOptions.ActionsEnabled"/> is off.
/// </summary>
public sealed class AssistantToolRegistry(IEnumerable<IAssistantTool> tools)
{
    public IReadOnlyList<IAssistantTool> GetToolsFor(AssistantCallerContext context) =>
        tools.Where(t => t.IsAvailableFor(context)).ToList();

    /// <summary>Looked up by name when the model requests a tool call -- returns null (not the raw
    /// tool) for anything outside this caller's filtered set, so a tool name the model wasn't even
    /// offered can never be executed even if it somehow asks for one anyway.</summary>
    public IAssistantTool? FindAvailable(string name, AssistantCallerContext context) =>
        GetToolsFor(context).FirstOrDefault(t => t.Name == name);
}
