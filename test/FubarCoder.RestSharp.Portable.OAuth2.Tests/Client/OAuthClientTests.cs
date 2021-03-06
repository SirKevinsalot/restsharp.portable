﻿using System;
using System.Linq;
using System.Collections.Specialized;
using System.Net;
using FizzWare.NBuilder;
using NSubstitute;
using NUnit.Framework;
using RestSharp.Portable.OAuth2;
using RestSharp.Portable.OAuth2.Client;
using RestSharp.Portable.OAuth2.Configuration;
using RestSharp.Portable.OAuth2.Infrastructure;
using RestSharp.Portable.OAuth2.Models;
using RestSharp.Portable;
using FluentAssertions;
using RestSharp.Portable.Authenticators;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using RestSharp.Portable.OAuth1;

namespace RestSharp.Portable.OAuth2.Tests.Client
{
    [TestFixture]
    public class OAuthClientTests
    {
        private IRequestFactory factory;
        private OAuthClientDescendant descendant;

        private static System.Text.Encoding _encoding = System.Text.Encoding.UTF8;

        [SetUp]
        public void SetUp()
        {
            factory = Substitute.For<IRequestFactory>();
            var client = Substitute.For<IRestClient>();
            var request = Substitute.For<IRestRequest>();
            var response = Substitute.For<IRestResponse>();
            factory.CreateClient().Returns(client);
            factory.CreateRequest(Arg.Any<string>()).ReturnsForAnyArgs(request);
            client.Execute(request).Returns(Task.FromResult(response));
            response.StatusCode.Returns(HttpStatusCode.OK);
            descendant = new OAuthClientDescendant(
                factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ThrowNotSupported_When_UserWantsToTransmitState()
        {
            descendant.Awaiting(x => x.GetLoginLinkUri("any state")).ShouldThrow<NotSupportedException>();
        }

        [Test]
        public async Task Should_ThrowUnexpectedResponse_When_StatusIsNotOk()
        {
            (await factory.CreateClient().Execute(factory.CreateRequest(null))).StatusCode.Returns(HttpStatusCode.InternalServerError);
            descendant.Awaiting(x => x.GetLoginLinkUri()).ShouldThrow<UnexpectedResponseException>();
        }

        [Test]
        public async Task Should_ThrowUnexpectedResponse_When_ContentIsEmpty()
        {
            (await factory.CreateClient().Execute(factory.CreateRequest(null))).RawBytes.Returns(_encoding.GetBytes(""));
            descendant.Awaiting(x => x.GetLoginLinkUri()).ShouldThrow<UnexpectedResponseException>();
        }

        [Test]
        public async Task Should_ThrowUnexpectedResponse_When_OAuthTokenIsEmpty()
        {
            (await factory.CreateClient().Execute(factory.CreateRequest(null))).RawBytes.Returns(_encoding.GetBytes("something=something_other"));
            descendant
                .Awaiting(x => x.GetLoginLinkUri())
                .ShouldThrow<UnexpectedResponseException>()
                .And.FieldName.Should().Be("oauth_token");
        }

        [Test]
        public async Task Should_ThrowUnexpectedResponse_When_OAuthSecretIsEmpty()
        {
            var response = await factory.CreateClient().Execute(factory.CreateRequest(null));
            response.RawBytes.Returns(_encoding.GetBytes("oauth_token=token"));
            response.Content.Returns("oauth_token=token");
            descendant
                .Awaiting(x => x.GetLoginLinkUri())
                .ShouldThrow<UnexpectedResponseException>()
                .And.FieldName.Should().Be("oauth_token_secret");
        }

        [Test]
        public async Task Should_IssueCorrectRequestForRequestToken_When_GetLoginLinkUriIsCalled()
        {
            // arrange
            var restClient = factory.CreateClient();
            var restRequest = factory.CreateRequest(null);
            var response = await restClient.Execute(restRequest);
            response.RawBytes.Returns(_encoding.GetBytes("any content to pass response verification"));
            response.Content.Returns("oauth_token=token&oauth_token_secret=secret");

            // act
            await descendant.GetLoginLinkUri();

            // assert
            factory.Received().CreateClient();
            factory.Received().CreateRequest("/RequestTokenServiceEndpoint");

            restClient.Received().BaseUrl = new Uri("https://RequestTokenServiceEndpoint");
            restRequest.Received().Method = Method.POST;

            restClient.Authenticator.Should().NotBeNull();
            restClient.Authenticator.Should().BeOfType<OAuth1Authenticator>();
        }

        [Test]
        public async Task Should_ComposeCorrectLoginUri_When_GetLoginLinkIsCalled()
        {
            // arrange
            var restClient = factory.CreateClient();
            var restRequest = factory.CreateRequest(null);
            var response = await restClient.Execute(restRequest);
            response.RawBytes.Returns(_encoding.GetBytes("any content to pass response verification"));
            response.Content.Returns("oauth_token=token5&oauth_token_secret=secret");

            // act
            var uri = await descendant.GetLoginLinkUri();

            // assert
            uri.Should().Be("https://loginserviceendpoint/");

            factory.Received().CreateClient();
            factory.Received().CreateRequest("/LoginServiceEndpoint");
            
            restClient.Received().BaseUrl = new Uri("https://LoginServiceEndpoint");
            restRequest.Parameters.Received().AddOrUpdate(Arg.Is<Parameter>(x => x.Name == "oauth_token" && (string)x.Value == "token5"));
        }

        [Test]
        public async Task Should_IssueCorrectRequestForAccessToken_When_GetUserInfoIsCalled()
        {
            // arrange
            var restClient = factory.CreateClient();
            var restRequest = factory.CreateRequest(null);
            var response = await restClient.Execute(restRequest);
            response.RawBytes.Returns(_encoding.GetBytes("any content to pass response verification"));
            response.Content.Returns("oauth_token=token5&oauth_token_secret=secret");

            // act
            await descendant.GetUserInfo(new Dictionary<string, string>
            {
                {"oauth_token", "token1"},
                {"oauth_verifier", "verifier100"}
            }.ToLookup(y => y.Key, y => y.Value));

            // assert
            factory.Received().CreateClient();
            factory.Received().CreateRequest("/AccessTokenServiceEndpoint");

            restClient.Received().BaseUrl = new Uri("https://AccessTokenServiceEndpoint");
            restRequest.Received().Method = Method.POST;
            
            restClient.Authenticator.Should().NotBeNull();
            restClient.Authenticator.Should().BeOfType<OAuth1Authenticator>();
        }

        [Test]
        public async Task Should_IssueCorrectRequestForUserInfo_When_GetUserInfoIsCalled()
        {
            // arrange
            var restClient = factory.CreateClient();
            var restRequest = factory.CreateRequest(null);
            var response = await restClient.Execute(restRequest);
            response.RawBytes.Returns(_encoding.GetBytes("any content to pass response verification"));
            response.Content.Returns("oauth_token=token&oauth_token_secret=secret", "abba");

            // act
            var info = await descendant.GetUserInfo(new Dictionary<string, string>
            {
                {"oauth_token", "token1"},
                {"oauth_verifier", "verifier100"}
            }.ToLookup(y => y.Key, y => y.Value));

            // assert
            factory.Received().CreateClient();
            factory.Received().CreateRequest("/UserInfoServiceEndpoint");

            restClient.Received().BaseUrl = new Uri("https://UserInfoServiceEndpoint");

            restClient.Authenticator.Should().NotBeNull();
            restClient.Authenticator.Should().BeAssignableTo<OAuth1Authenticator>();

            info.Id.Should().Be("abba");
        }

        class OAuthClientDescendant : OAuthClient
        {
            public OAuthClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration)
            {
            }
            
            protected override Endpoint RequestTokenServiceEndpoint
            {
                get
                {
                    return new Endpoint
                    {
                        BaseUri = "https://RequestTokenServiceEndpoint",
                        Resource = "/RequestTokenServiceEndpoint"
                    };
                }
            }

            protected override Endpoint LoginServiceEndpoint
            {
                get
                {
                    return new Endpoint
                    {
                        BaseUri = "https://LoginServiceEndpoint",
                        Resource = "/LoginServiceEndpoint"
                    };
                }
            }

            protected override Endpoint AccessTokenServiceEndpoint
            {
                get
                {
                    return new Endpoint
                    {
                        BaseUri = "https://AccessTokenServiceEndpoint",
                        Resource = "/AccessTokenServiceEndpoint"
                    };
                }
            }

            protected override Endpoint UserInfoServiceEndpoint
            {
                get
                {
                    return new Endpoint
                    {
                        BaseUri = "https://UserInfoServiceEndpoint",
                        Resource = "/UserInfoServiceEndpoint"
                    };
                }
            }

            public override string Name
            {
                get { return "OAuthClientTest"; }
            }

            protected override UserInfo ParseUserInfo(string content)
            {
                return Builder<UserInfo>.CreateNew()
                    .With(x => x.Id = content)
                    .Build();
            }
        }
    }

}