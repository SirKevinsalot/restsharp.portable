﻿using System;
using System.Net;
using System.Net.Http;

namespace RestSharp.Portable.Authenticators
{
    /// <summary>
    /// The HTTP Basic authenticator that works for hidden pages (status code 404).
    /// </summary>
    public class HttpHiddenBasicAuthenticator : HttpBasicAuthenticator
    {
        /// <summary>
        /// Determines if the authentication module can handle the challenge sent with the response.
        /// </summary>
        /// <param name="client">The REST client the response is assigned to</param>
        /// <param name="request">The REST request the response is assigned to</param>
        /// <param name="response">The response that returned the authentication challenge</param>
        /// <returns>true when the authenticator can handle the sent challenge</returns>
        public override bool CanHandleChallenge(IRestClient client, IRestRequest request, HttpResponseMessage response)
        {
            if (HasAuthorizationToken)
                return false;
            if (response.StatusCode == HttpStatusCode.NotFound)
                return true;
            return base.CanHandleChallenge(client, request, response);
        }
    }
}
