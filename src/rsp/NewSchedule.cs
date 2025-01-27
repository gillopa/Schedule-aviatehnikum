using MediatR;

namespace rsp;

class NewSchedule : INotification
{
    public required Schedule Schedule { get; init; }
}