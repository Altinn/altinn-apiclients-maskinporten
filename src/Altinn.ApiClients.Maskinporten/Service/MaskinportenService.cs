﻿using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ApiClients.Maskinporten.Services
{
    public class MaskinportenService: IMaskinporten
    {
        private readonly HttpClient _client;

        private readonly MaskinportenSettings _maskinportenConfig;

        public MaskinportenService(HttpClient httpClient, IOptions<MaskinportenSettings> maskinportenConfig)
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client = httpClient;
            _maskinportenConfig = maskinportenConfig.Value;
        }

     
        public async Task<TokenResponse> GetToken(X509Certificate2 cert, string clientId, string scope, string resource)
        {
            return await GetToken(cert, null, clientId, scope, resource);
        }

        public async Task<TokenResponse> GetToken(JsonWebKey jwk, string clientId, string scope, string resource)
        {
            return await GetToken(null, jwk, clientId, scope, resource);
        }

        public async Task<TokenResponse> GetToken(string base64EncodedJwk, string clientId, string scope, string resource)
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedJwk);
            string jwkjson = Encoding.UTF8.GetString(base64EncodedBytes);
            JsonWebKey jwk = new JsonWebKey(jwkjson);
            return await GetToken(null, jwk, clientId, scope, resource);
        }


        private async Task<TokenResponse> GetToken(X509Certificate2 cert, JsonWebKey jwk, string clientId, string scope, string resource)
        {
            string jwtAssertion = GetJwtAssertion(cert, jwk, clientId, scope, resource);
            FormUrlEncodedContent content = GetUrlEncodedContent(jwtAssertion);
            return await PostToken(content);
        }

        public string GetJwtAssertion(X509Certificate2 cert, JsonWebKey jwk, string clientId, string scope, string resource)
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
            JwtHeader header;
            if (cert != null)
            {
                header = GetHeader(cert);
            }
            else
            {
                header = GetHeader(jwk);
            }

            JwtPayload payload = new JwtPayload
            {
                { "aud", GetAssertionAud() },
                { "scope", scope },
                { "iss", clientId },
                { "exp", dateTimeOffset.ToUnixTimeSeconds() + 10 },
                { "iat", dateTimeOffset.ToUnixTimeSeconds() },
                { "jti", Guid.NewGuid().ToString() },
            };
            
            if (string.IsNullOrEmpty(resource))
            {
                payload.Add("resource", resource);
            }

            JwtSecurityToken securityToken = new JwtSecurityToken(header, payload);
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(securityToken);
        }

        private JwtHeader GetHeader(JsonWebKey jwk)
        {
            JwtHeader header = new JwtHeader(new SigningCredentials(jwk, SecurityAlgorithms.RsaSha256));
            return header;
        }

        private JwtHeader GetHeader(X509Certificate2 cert)
        {
            X509SecurityKey securityKey = new X509SecurityKey(cert);
            JwtHeader header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
                { "x5c", new List<string>() { Convert.ToBase64String(cert.GetRawCertData()) } }
            };
            header.Remove("typ");
            header.Remove("kid");
            return header;
        }

        private FormUrlEncodedContent GetUrlEncodedContent(string assertion)
        {
            FormUrlEncodedContent formContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("assertion", assertion),
            });

            return formContent;
        }

        public async Task<TokenResponse> PostToken(FormUrlEncodedContent bearer)
        {
            TokenResponse token = null; ;

            HttpRequestMessage requestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(GetTokenEndpoint()),
                Content = bearer
            };

            HttpResponseMessage response = await _client.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                token = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(content);
                return token;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
            }

            return null;
        }

        private string GetAssertionAud()
        {
            if (_maskinportenConfig.Environment.Equals("prod"))
            {
                return _maskinportenConfig.JwtAssertionAudienceProd;
            }
            else if (_maskinportenConfig.Environment.Equals("ver1"))
            {
                return _maskinportenConfig.JwtAssertionAudienceVer1;
            }
            else if (_maskinportenConfig.Environment.Equals("ver2"))
            {
                return _maskinportenConfig.JwtAssertionAudienceVer2;
            }

            return null;
        }

        private string GetTokenEndpoint()
        {
           if(_maskinportenConfig.Environment.Equals("prod"))
            {
                return _maskinportenConfig.TokenEndpointProd;
            }
            else if (_maskinportenConfig.Environment.Equals("ver1"))
            {
                return _maskinportenConfig.TokenEndpointVer1;
            }
            else if (_maskinportenConfig.Environment.Equals("ver2"))
            {
                return _maskinportenConfig.TokenEndpointVer2;
            }

           return null;
        }

    }
}
