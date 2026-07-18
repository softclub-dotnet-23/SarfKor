using Domain.Feedback;

namespace Application.Abstractions;

public interface IReviewReplyRepository
{
    void Add(ReviewReply reply);
}
