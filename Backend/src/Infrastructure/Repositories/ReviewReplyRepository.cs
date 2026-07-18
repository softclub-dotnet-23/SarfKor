using Application.Abstractions;
using Domain.Feedback;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class ReviewReplyRepository(AppDbContext dbContext) : IReviewReplyRepository
{
    public void Add(ReviewReply reply) => dbContext.ReviewReplies.Add(reply);
}
