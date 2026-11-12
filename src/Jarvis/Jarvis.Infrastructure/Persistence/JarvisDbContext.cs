using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using Jarvis.Domain.Conversations;
using Jarvis.Domain.Runs;
using Jarvis.Domain.ToolCalls;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jarvis.Infrastructure.Persistence;

/// <summary>JARVIS's own database (<c>homeapps_jarvis</c>): the log of runs and their tool calls (jarvis-v1.md 2.5)
/// and the people's conversations from the app (2.4).</summary>
public sealed class JarvisDbContext(DbContextOptions<JarvisDbContext> options, IPublisher publisher)
    : HomeAppsDbContext(options, publisher)
{
    public DbSet<AssistantRun> Runs => Set<AssistantRun>();

    public DbSet<ToolCall> ToolCalls => Set<ToolCall>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
}
