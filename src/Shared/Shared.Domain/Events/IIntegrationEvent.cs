namespace Shared.Domain.Events;

/// <summary>
/// Mikroservisler arası asenkron iletişim için integration event kontratı.
/// ISP: Her event yalnızca bu minimal arayüzü implement eder.
/// OCP: Yeni event tipleri bu arayüzü implement ederek sisteme açık şekilde eklenir.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}
