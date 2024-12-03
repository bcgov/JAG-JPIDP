namespace jumwebapi.Data.Seed;

using jumwebapi.Infrastructure.HttpClients.Keycloak;
using Microsoft.EntityFrameworkCore;



public class IdentityProviderDataSeeder(IKeycloakAdministrationClient keycloakClient
                                        , ILogger<IdentityProviderDataSeeder> logger
                                        , JumDbContext context)
{
    public async Task Seed()
    {
        context.Database.EnsureCreated();
        await context.Database.MigrateAsync();

    }
}
