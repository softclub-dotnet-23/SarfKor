using System.Threading.Channels;
using Application.Abstractions;

namespace Infrastructure.Email;

/// <summary>Unbounded in-memory channel of pending email sends — keeps QueuedEmailSender's methods
/// synchronous-looking-but-instant (just a channel write) while EmailSenderBackgroundService drains
/// it against the real SmtpEmailSender off the request thread. In-memory means a process
/// crash/restart drops anything still queued — acceptable for this kind of non-critical
/// transactional mail (invites, resets): the alternative this replaces was a request that could
/// 500/timeout the caller outright, which is strictly worse than an occasional dropped resend the
/// user can just click again.</summary>
public sealed class EmailJobQueue
{
    private readonly Channel<Func<IEmailSender, CancellationToken, Task>> _channel =
        Channel.CreateUnbounded<Func<IEmailSender, CancellationToken, Task>>();

    public void Enqueue(Func<IEmailSender, CancellationToken, Task> job) => _channel.Writer.TryWrite(job);

    public ChannelReader<Func<IEmailSender, CancellationToken, Task>> Reader => _channel.Reader;
}
