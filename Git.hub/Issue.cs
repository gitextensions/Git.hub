using System;
using System.Collections.Generic;
using System.Diagnostics;
using RestSharp;

namespace Git.hub
{
    [DebuggerDisplay("{Number}:{Title}")]
    public class Issue
    {
        internal RestClient _client;

        public int Number { get; internal set; }
        public string Title { get; internal set; }
        public string Body { get; internal set; }
        public DateTime CreatedAt { get; internal set; }
        public DateTime UpdatedAt { get; internal set; }
        public string Url { get; internal set; }
        public User User { get; internal set; }
        public Repository Repository { get; internal set; }

        public IReadOnlyList<IssueComment> GetComments()
        {
            var request = new RestRequest("/repos/{user}/{repo}/issues/{issue}/comments");
            request.AddUrlSegment("user", Repository.Owner.Login);
            request.AddUrlSegment("repo", Repository.Name);
            request.AddUrlSegment("issue", Number.ToString());

            return _client.GetList<IssueComment>(request);
        }

        public IssueComment CreateComment(string body)
        {
            if (_client.Authenticator == null)
                throw new ArgumentException("no authentication details");

            var request = new RestRequest("/repos/{user}/{repo}/issues/{issue}/comments");
            request.AddUrlSegment("user", Repository.Owner.Login);
            request.AddUrlSegment("repo", Repository.Name);
            request.AddUrlSegment("issue", Number.ToString());

            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(new {
                body = body
            });
            return _client.Post<IssueComment>(request).Data;
        }
    }

    public class IssueComment
    {
        public int Id { get; private set; }
        public string Body { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public User User { get; private set; }

        /// <summary>
        /// api.github.com/repos/{user}/{repo}/issues/{issue}/comments/{id}
        /// </summary>
        public string Url { get; private set; }
    }
}
