using Mapster;
using server.src.Dtos;
using server.src.Models;

namespace server.src.Mapper;

public class NotificationMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Notification, NotificationResponseDto>();
    }
}
