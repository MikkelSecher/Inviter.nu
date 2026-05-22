namespace Inviter.Api.Features.Events;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api");

        api.MapPost("/events", CreateEvent.Handle);
        api.MapGet("/invite/{inviteToken}", GetEventPublic.Handle);
        api.MapGet("/manage/{adminToken}", GetEventAdmin.Handle);
        api.MapPut("/manage/{adminToken}", UpdateEvent.Handle);
    }
}
