﻿using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using RestSharp.Portable.OAuth2.Client;
using RestSharp.Portable.OAuth2;
using RestSharp.Portable.OAuth2.Configuration;
using RestSharp.Portable.OAuth2.Infrastructure;
using RestSharp.Portable.OAuth2.Models;
using RestSharp.Portable;

namespace RestSharp.Portable.OAuth2.Tests.Client.Impl
{
    [TestFixture]
    public class LinkedInClientTests
    {
        private const string Content = "{" +
                                        " \"id\": \"id\"," +
                                        " \"firstName\": \"firstname\"," +
                                        " \"lastName\": \"lastname\"," +
                                        " \"pictureUrl\": \"pictureurl\"" +
                                        "}";

        private LinkedInClientDescendant descendant;

        [SetUp]
        public void SetUp()
        {
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient().Returns(Substitute.For<IRestClient>());
            factory.CreateRequest(null).Returns(Substitute.For<IRestRequest>());

            descendant = new LinkedInClientDescendant(factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.linkedin.com");
            endpoint.Resource.Should().Be("/uas/oauth2/authorization");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.linkedin.com");
            endpoint.Resource.Should().Be("/uas/oauth2/accessToken");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.linkedin.com");
            endpoint.Resource.Should().Be("/v1/people/~:(id,email-address,first-name,last-name,picture-url)?format=json");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("id");
            info.FirstName.Should().Be("firstname");
            info.LastName.Should().Be("lastname");
            info.PhotoUri.Should().Be("pictureurl");
        }

        class LinkedInClientDescendant : LinkedInClient
        {
            public LinkedInClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration)
            {
            }

            public Endpoint GetAccessCodeServiceEndpoint()
            {
                return AccessCodeServiceEndpoint;
            }

            public Endpoint GetAccessTokenServiceEndpoint()
            {
                return AccessTokenServiceEndpoint;
            }

            public Endpoint GetUserInfoServiceEndpoint()
            {
                return UserInfoServiceEndpoint;
            }

            public new UserInfo ParseUserInfo(string content)
            {
                return base.ParseUserInfo(content);
            }
        }  
    }
}
