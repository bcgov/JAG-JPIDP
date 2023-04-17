namespace PidpTests.Features.Parties;

using FakeItEasy;
using NodaTime;
using System.Security.Claims;
using Xunit;

using Pidp.Extensions;
using static Pidp.Features.Parties.ProfileStatus;
using static Pidp.Features.Parties.ProfileStatus.Model;
using Pidp.Models;
using Pidp.Models.Lookups;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.HttpClients.Plr;
using PidpTests.TestingExtensions;

public class ProfileStatusSAEformsTests : ProfileStatusTest
{
    [Theory]
    [MemberData(nameof(AllIdpsUserTestCases))]
    public async void HandleAsync_NoProfile_LockedOrHidden(ClaimsPrincipal user)
    {
        var party = this.TestDb.Has(AParty.WithNoProfile(user.GetIdentityProvider()));
        var client = A.Fake<IPlrClient>()
            .ReturningAStatandingsDigest(PlrStandingsDigest.FromEmpty());
        var handler = this.MockDependenciesFor<CommandHandler>(client);

        var profile = await handler.HandleAsync(new Command { Id = party.Id, User = user });

        var eforms = profile.Section<SAEforms>();
        eforms.AssertNoAlerts();
        var expected = user.GetIdentityProvider() == ClaimValues.BCServicesCard
            ? StatusCode.Locked
            : StatusCode.Hidden;
        Assert.Equal(expected, eforms.StatusCode);
        Assert.False(eforms.IncorrectLicenceType);
    }

    [Theory]
    [MemberData(nameof(AllIdpsUserTestCases))]
    public async void HandleAsync_Demographics_LockedOrHidden(ClaimsPrincipal user)
    {
        var party = this.TestDb.Has(AParty.WithDemographics(user.GetIdentityProvider()));
        var client = A.Fake<IPlrClient>()
            .ReturningAStatandingsDigest(PlrStandingsDigest.FromEmpty());
        var handler = this.MockDependenciesFor<CommandHandler>(client);

        var profile = await handler.HandleAsync(new Command { Id = party.Id, User = user });

        var eforms = profile.Section<SAEforms>();
        eforms.AssertNoAlerts();
        var expected = user.GetIdentityProvider() == ClaimValues.BCServicesCard
            ? StatusCode.Locked
            : StatusCode.Hidden;
        Assert.Equal(expected, eforms.StatusCode);
        Assert.False(eforms.IncorrectLicenceType);
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async void HandleAsync_BcscAccessRequested_Complete(bool standing)
    {
        var party = this.TestDb.Has(AParty.WithLicenceDeclared());
        party.AccessRequests = new[] { new AccessRequest { AccessTypeCode = AccessTypeCode.SAEforms } };
        this.TestDb.SaveChanges();
        var client = A.Fake<IPlrClient>()
            .ReturningAStatandingsDigest(standing);
        var handler = this.MockDependenciesFor<CommandHandler>(client);

        var profile = await handler.HandleAsync(new Command { Id = party.Id, User = AMock.BcscUser() });

        var eforms = profile.Section<SAEforms>();
        eforms.AssertNoAlerts();
        Assert.Equal(StatusCode.Complete, eforms.StatusCode);
        Assert.False(eforms.IncorrectLicenceType);
    }
}
