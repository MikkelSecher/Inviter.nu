namespace Inviter.Api.Features.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/admin");
        api.MapGet("/{slug}/metrics", GetMetricsSnapshot.Handle);
    }
}
