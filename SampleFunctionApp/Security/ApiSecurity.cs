﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AzureFunctionsOpenIDConnectAuthSample.Security.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AzureFunctionsOpenIDConnectAuthSample.Security
{
    public class ApiSecurity : IApiSecurity
    {
        private readonly IAuthorizationHeaderBearerTokenParser _authorizationHeaderBearerTokenParser;

        private readonly IJwtSecurityTokenHandlerWrapper _jwtSecurityTokenHandlerWrapper;

        private readonly IOidcConfigurationManager _oidcConfigurationManager;

        private readonly string _issuerUrl = null;
        private readonly string _audience = null;

        public ApiSecurity(
            IOptions<ApiSecuritySettings> apiSecuritySettingsOptions,
            IAuthorizationHeaderBearerTokenParser authorizationHeaderBearerTokenParser,
            IJwtSecurityTokenHandlerWrapper jwtSecurityTokenHandlerWrapper,
            IOidcConfigurationManagerFactory oidcConfigurationManagerFactory)
        {
            apiSecuritySettingsOptions.Value.ThrowIfInvalid();

            _issuerUrl = apiSecuritySettingsOptions.Value.AuthorizationIssuerUrl;
            _audience = apiSecuritySettingsOptions.Value.AuthorizationAudience;

            _authorizationHeaderBearerTokenParser = authorizationHeaderBearerTokenParser;

            _jwtSecurityTokenHandlerWrapper = jwtSecurityTokenHandlerWrapper;

            _oidcConfigurationManager = oidcConfigurationManagerFactory.New(_issuerUrl);
        }

        public async Task<AuthorizationResult> Authorize(IHeaderDictionary httpRequestHeaders, ILogger log)
        {
            string authorizationBearerToken = _authorizationHeaderBearerTokenParser.ParseToken(httpRequestHeaders);
            if (authorizationBearerToken == null)
            {
                return new AuthorizationResult("Authorization header is missing or is not a Bearer token.");
            }

            ClaimsPrincipal claimsPrincipal = null;
            SecurityToken securityToken = null;

            int validationRetryCount = 0;

            do
            {
                IEnumerable<SecurityKey> isserSigningKeys = null;
                try
                {
                    // Get the cached signing keys if they were retrieved previously. If they haven't been retrieved,
                    // or the cached keys are stale, then a fresh set of signing keys are retrieved
                    // from the OpenID Connect provider (issuer) cached and returned.
                    // This method will throw if the configuration cannot be retrieved, instead of returning null.
                    isserSigningKeys = await _oidcConfigurationManager.GetIssuerSigningKeysAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        "Problem getting signing keys from Open ID Connect provider (issuer) via ConfigurationManager.",
                        ex);
                }

                var tokenValidationParameters = new TokenValidationParameters
                {
                    RequireSignedTokens = true,
                    ValidAudience = _audience,
                    ValidateAudience = true,
                    ValidIssuer = _issuerUrl,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    IssuerSigningKeys = isserSigningKeys
                };

                try
                {
                    // Try to validate the token.
                    // Throws if the the token cannot be validated.
                    // If the token is successfully validiate then the ClaimsPrincipal from the JWT is returned.
                    // The ClaimsPrincipal returned does not include claims found in the JWT header.
                    claimsPrincipal = _jwtSecurityTokenHandlerWrapper.ValidateToken(
                        authorizationBearerToken,
                        tokenValidationParameters,
                        out securityToken);
                }
                catch (SecurityTokenSignatureKeyNotFoundException keyNotFoundException)
                {
                    // A SecurityTokenSignatureKeyNotFoundException is thrown if the signature key for validating
                    // JWT tokens could not be found. This could happen if the issuer has changed the signing keys
                    // since the last time they were retreived by the ConfigurationManager.
                    // To handle this we ask the ConfigurationManger to refresh which causes it to retreive the keys
                    // again, and then we retry the validation. We only retry once.

                    log.LogWarning(
                        keyNotFoundException,
                        "Exception validating token. JWT signature key not found."
                        + $" {(validationRetryCount == 0 ? "Retrying..." : "Refresh Retry Failed!")}.");

                    _oidcConfigurationManager.RequestRefresh();

                    validationRetryCount++;
                }
                catch (SecurityTokenException tokenException)
                {
                    log.LogWarning(
                        tokenException,
                        $"Authorization Failed. Exception caught while validating token.");

                    return new AuthorizationResult("Authorization Failed.");
                }
            } while (claimsPrincipal == null && validationRetryCount <= 1);

            return new AuthorizationResult(claimsPrincipal, securityToken);
        }
    }
}