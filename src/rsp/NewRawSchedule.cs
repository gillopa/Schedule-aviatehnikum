using MediatR;

namespace rsp;

class NewRawSchedule : INotification
{
    public required byte[] Data { get; init; }
}