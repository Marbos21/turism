namespace FamilyTrip.Application.AI;

public interface IAiChatClient
{
    Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default);
}
