﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

using RestSharp.Portable.Authenticators;
using RestSharp.Portable.HttpClient;

using Xunit;

namespace RestSharp.Portable.Test.HttpClientTests
{
    public class IssueTests
    {
        [Fact(DisplayName = "HttpClient - Issue 12, Post 1 parameter")]
        public async Task TestIssue12_Post1()
        {
            using (var client = new RestClient("http://httpbin.org/"))
            {
                var tmp = new string('a', 70000);

                var request = new RestRequest("post", Method.POST);
                request.AddParameter("param1", tmp);

                var response = await client.Execute<PostResponse>(request);
                Assert.NotNull(response.Data);
                Assert.NotNull(response.Data.Form);
                Assert.True(response.Data.Form.ContainsKey("param1"));
                Assert.Equal(70000, response.Data.Form["param1"].Length);
                Assert.Equal(tmp, response.Data.Form["param1"]);
            }
        }

        [Fact(DisplayName = "HttpClient - Issue 12, Post 2 parameters")]
        public async Task TestIssue12_Post2()
        {
            using (var client = new RestClient("http://httpbin.org/"))
            {
                var tmp = new string('a', 70000);

                var request = new RestRequest("post", Method.POST);
                request.AddParameter("param1", tmp);
                request.AddParameter("param2", "param2");

                var response = await client.Execute<PostResponse>(request);
                Assert.NotNull(response.Data);
                Assert.NotNull(response.Data.Form);
                Assert.True(response.Data.Form.ContainsKey("param1"));
                Assert.Equal(70000, response.Data.Form["param1"].Length);
                Assert.Equal(tmp, response.Data.Form["param1"]);

                Assert.True(response.Data.Form.ContainsKey("param2"));
                Assert.Equal("param2", response.Data.Form["param2"]);
            }
        }

        [Fact(DisplayName = "HttpClient - Issue 16")]
        public void TestIssue16()
        {
            using (var client = new RestClient("http://httpbin.org/"))
            {
                var request = new RestRequest("get?a={a}");
                request.AddParameter("a", "value-of-a", ParameterType.UrlSegment);

                Assert.Equal("http://httpbin.org/get?a=value-of-a", client.BuildUri(request).ToString());
            }
        }

        [Fact(DisplayName = "HttpClient - Issue 19")]
        public void TestIssue19()
        {
            using (var client = new RestClient("http://httpbin.org/"))
            {
                var req1 = new RestRequest("post", Method.POST);
                req1.AddParameter("a", "value-of-a");
                var t1 = client.Execute<PostResponse>(req1);

                var req2 = new RestRequest("post", Method.POST);
                req2.AddParameter("ab", "value-of-ab");
                var t2 = client.Execute<PostResponse>(req2);

                Task.WaitAll(t1, t2);

                Assert.NotNull(t1.Result.Data);
                Assert.NotNull(t1.Result.Data.Form);
                Assert.True(t1.Result.Data.Form.ContainsKey("a"));
                Assert.Equal("value-of-a", t1.Result.Data.Form["a"]);

                Assert.NotNull(t2.Result.Data);
                Assert.NotNull(t2.Result.Data.Form);
                Assert.True(t2.Result.Data.Form.ContainsKey("ab"));
                Assert.Equal("value-of-ab", t2.Result.Data.Form["ab"]);
            }
        }

        [Fact(DisplayName = "HttpClient - Issue 23")]
        public async Task TestIssue23()
        {
            using (var client = new RestClient("http://httpbin.org/"))
            {
                client.Authenticator = new HttpBasicAuthenticator();
                client.Credentials = new NetworkCredential("foo", "bar");
                var request = new RestRequest("post", Method.GET);
                request.AddJsonBody("foo");
                await client.Execute(request);
            }
        }

        [Fact(DisplayName = "HttpClient - Issue 25")]
        public void TestIssue25()
        {
            using (var client = new RestClient("http://httpbin.org/"))
            {
                var req1 = new RestRequest("post", Method.POST);
                req1.AddParameter("a", "value-of-a");

                var req2 = new RestRequest("post", Method.POST);
                req2.AddParameter("ab", "value-of-ab");

                var t1 = client.Execute<PostResponse>(req1);
                var t2 = client.Execute<PostResponse>(req2);
                Task.WaitAll(t1, t2);

                Assert.NotNull(t1.Result.Data);
                Assert.NotNull(t1.Result.Data.Form);
                Assert.True(t1.Result.Data.Form.ContainsKey("a"));
                Assert.Equal("value-of-a", t1.Result.Data.Form["a"]);

                Assert.NotNull(t2.Result.Data);
                Assert.NotNull(t2.Result.Data.Form);
                Assert.True(t2.Result.Data.Form.ContainsKey("ab"));
                Assert.Equal("value-of-ab", t2.Result.Data.Form["ab"]);
            }
        }

        [Fact(DisplayName = "HttpClient - Issue 29 ContentCollectionMode = MultiPart")]
        public async Task TestIssue29_CollectionModeMultiPart()
        {
            using (var client = new RestClient("http://httpbin.org/"))
            {
                var req = new RestRequest("post", Method.POST);
                req.AddParameter("a", "value-of-a");
                req.ContentCollectionMode = ContentCollectionMode.MultiPart;
                var resp = await client.Execute<PostResponse>(req);
                Assert.NotNull(resp.Data);
                Assert.NotNull(resp.Data.Form);
                Assert.True(resp.Data.Form.ContainsKey("a"));
                Assert.Equal("value-of-a", resp.Data.Form["a"]);
            }
        }

        [Fact(DisplayName = "HttpClient - Issue 29 ContentType as Parameter")]
        public async Task TestIssue29_ContentTypeParameter()
        {
            using (var client = new RestClient("http://httpbin.org/"))
            {
                var req = new RestRequest("post", Method.POST);
                req.AddParameter("a", "value-of-a");
                req.AddHeader("content-type", "application/x-www-form-urlencoded;charset=utf-8");
                var resp = await client.Execute<PostResponse>(req);
                Assert.NotNull(resp.Data);
                Assert.NotNull(resp.Data.Form);
                Assert.True(resp.Data.Form.ContainsKey("a"));
                Assert.Equal("value-of-a", resp.Data.Form["a"]);
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "ReSharper bug")]
        private class PostResponse
        {
            public Dictionary<string, string> Form { get; set; }

            public object Json { get; set; }
        }
    }
}
