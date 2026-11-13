using __PROJECT__.Application.DTOs.General;

namespace __PROJECT__.Application.Interfaces;

public interface IApiActivityLogQueue
{
    bool TryEnqueue(ApiActivityLogEntry entry);
}
