namespace Inviter.Api.Features.Invitees;

public static class InviteeEndpoints
{
    public static void MapInviteeEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api");

        api.MapGet("/invite/{inviteToken}/invitee/{inviteeId:guid}", GetInviteePrefill.Handle);
        api.MapGet("/manage/{adminToken}/invitees", ListInvitees.Handle);
        api.MapPost("/manage/{adminToken}/invitees", AddInvitees.Handle);
        api.MapDelete("/manage/{adminToken}/invitees/{inviteeId:guid}", DeleteInvitee.Handle);
        api.MapPost("/manage/{adminToken}/invitees/send", SendInvitations.Handle);
    }
}
