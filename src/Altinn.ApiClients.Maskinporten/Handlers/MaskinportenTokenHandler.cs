﻿using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Models;
using Altinn.ApiClients.Maskinporten.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Altinn.ApiClients.Maskinporten.Service;

namespace Altinn.ApiClients.Maskinporten.Handlers
{
    public class MaskinportenTokenHandler<T> : MaskinportenTokenHandler, IMaskinportenTokenHandler<T> where T : ICustomClientSecret
    {
        public MaskinportenTokenHandler(
            IOptions<MaskinportenSettings<T>> maskinportenSettings, 
            IMaskinportenService<T> maskinporten) : base(maskinportenSettings, maskinporten)
        {
        }
    }
    public class MaskinportenTokenHandler : DelegatingHandler, IMaskinportenTokenHandler
    {
        private IMaskinporten _maskinporten;

        public MaskinportenTokenHandler(IMaskinporten maskinporten)
        {
            _maskinportenSettings = maskinportenSettings.Value;
            _maskinporten = maskinporten;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization == null)
            {
                TokenResponse tokenResponse = await GetTokenResponse(cancellationToken);
                if (tokenResponse != null)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
            {
                TokenResponse tokenResponse = await RefreshTokenResponse(cancellationToken);
                if (tokenResponse != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
                    response = await base.SendAsync(request, cancellationToken);
                }
            }

            return response;
        }

        private async Task<TokenResponse> GetTokenResponse(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return null;
            TokenResponse tokenResponse =  await _maskinporten.GetToken();
            return tokenResponse;
        }

        private async Task<TokenResponse> RefreshTokenResponse(CancellationToken cancellationToken)
        {
                if (cancellationToken.IsCancellationRequested) return null;
                 TokenResponse tokenResponse = await _maskinporten.GetToken(true);
                return tokenResponse;
        }
    }
}
