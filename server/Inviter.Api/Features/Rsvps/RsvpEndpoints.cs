namespace Inviter.Api.Features.Rsvps;

public static class RsvpEndpoints
{
    public static void MapRsvpEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api");

        api.MapPost("/invite/{inviteToken}/rsvp", SubmitRsvp.Handle);
        api.MapPut("/manage/{adminToken}/rsvp/{rsvpId:guid}/invitee", LinkRsvpInvitee.Handle);
        api.MapDelete("/manage/{adminToken}/rsvp/{rsvpId:guid}", DeleteRsvp.Handle);
    }
}
