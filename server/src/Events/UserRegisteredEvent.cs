namespace server.src.Events;

public record UserRegisteredEvent(int UserId, string Username, string Email);
