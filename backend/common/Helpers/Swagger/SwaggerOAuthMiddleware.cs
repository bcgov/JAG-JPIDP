namespace Common.Helpers.Swagger;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

public class SwaggerOAuthMiddleware
{
    private readonly RequestDelegate next;
    public SwaggerOAuthMiddleware(RequestDelegate next)
    {
        this.next = next;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsSwaggerUI(context.Request.Path))
        {
            // if user is not authenticated
            if (!context.User.Identity.IsAuthenticated)
            {
                await context.ChallengeAsync();
                return;
            }
        }
        await this.next.Invoke(context);
    }
    public static bool IsSwaggerUI(PathString pathString) => pathString.StartsWithSegments("/swagger");
}
