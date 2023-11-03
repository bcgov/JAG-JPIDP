namespace Common.Authorization;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Common.Exceptions;
using Common.Models.Authorization;
using IdentityModel.Client;

public class TokenService
{

    private readonly string tokenEndpoint;

    private Dictionary<string, TokenModel> accessTokens = new Dictionary<string, TokenModel>();


    public TokenService(string tokenEndpoint) =>
        this.tokenEndpoint = tokenEndpoint ?? throw new DIAMAuthException("Null token endpoint passed to TokenService()");



    public async Task<TokenModel> GetToken(string clientId, string clientSecret)
    {

        if (this.accessTokens.TryGetValue(clientId, out var tokenModel))
        {
            if (tokenModel != null)
            {
                if (tokenModel.Expires <= DateTime.UtcNow)
                {
                    // token expired
                    this.accessTokens.Remove(clientId);
                }
                else
                {
                    return tokenModel;
                }
            }
        }

        var accessTokenClient = new HttpClient();
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var accessToken = await accessTokenClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = this.tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                GrantType = "client_credentials"
            });



            var jwtSecurityToken = handler.ReadJwtToken(accessToken.AccessToken);

            var tokenTicks = GetTokenExpirationTime(jwtSecurityToken);
            var subject = GetTokenSubject(jwtSecurityToken);
            var tokenDate = DateTimeOffset.FromUnixTimeSeconds(tokenTicks);
            var timeSpan = new DateTime() - tokenDate;
            var ms = tokenDate.ToUnixTimeMilliseconds();
            var token = new TokenModel
            {
                Token = accessToken.AccessToken,
                Subject = subject,
                Expires = GetTokenExpirationDateTime(jwtSecurityToken),
            };
            this.accessTokens.Add(clientId, token);
            return token;
        }
        catch (Exception ex)
        {
            throw new DIAMAuthException($"Failed to get token for client {clientId}", ex);
        }
    }

    public async Task<string> GetTokenString(string clientId, string clientSecret)
    {
        var token = await this.GetToken(clientId, clientSecret);


        return token.Token.ToString();

    }

    private static DateTime GetTokenExpirationDateTime(JwtSecurityToken jwtSecurityToken) => jwtSecurityToken.ValidTo;

    private static long GetTokenExpirationTime(JwtSecurityToken jwtSecurityToken)
    {


        var tokenExp = jwtSecurityToken.Claims.First(claim => claim.Type.Equals("exp", StringComparison.Ordinal)).Value;
        var ticks = long.Parse(tokenExp, CultureInfo.InvariantCulture);
        return ticks;
    }

    private static string GetTokenSubject(JwtSecurityToken jwtSecurityToken) => jwtSecurityToken.Claims.First(claim => claim.Type.Equals("sub", StringComparison.Ordinal)).Value;

}
