﻿using System;
using System.Collections.Generic;
using Git.hub.util;
using RestSharp;

namespace Git.hub
{
    public class Repository
    {
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public string Homepage { get; internal set; }
        public string DefaultBranch { get; internal set; }
        public User Owner { get; internal set; }
        public bool Fork { get; internal set; }
        public int Forks { get; internal set; }
        public bool Private { get; internal set; }
        public Organization Organization { get; internal set; }

        private Repository _Parent;
        public Repository Parent
        {
            get
            {
                if (!Detailed)
                    throw new NotSupportedException();
                return _Parent;
            }
            private set
            {
                _Parent = value;
            }
        }

        /// <summary>
        /// Read/Write clone url via SSH
        /// git@github.com/{user}/{repo.git}
        /// </summary>
        public string SshUrl { get; internal set; }

        /// <summary>
        /// Read/Write clone url via HTTPS
        /// https://github.com/{user}/{repo}.git
        /// </summary>
        public string CloneUrl { get; internal set; }

        internal RestClient _client;

        /// <summary>
        /// true if fetched from github.com/{user}/{repo}, false if from github.com/{user}
        /// </summary>
        public bool Detailed { get; internal set; }

        /// <summary>
        /// Forks this repository into your own account.
        /// </summary>
        /// <returns></returns>
        public Repository CreateFork()
        {
            RestRequest request = new("/repos/{user}/{repo}/forks");
            request.AddUrlSegment("user", Owner.Login);
            request.AddUrlSegment("repo", Name);

            Repository forked = _client.Post<Repository>(request).Data;
            forked._client = _client;
            return forked;
        }

        /// <summary>
        /// Lists all branches
        /// </summary>
        /// <remarks>Not really sure if that's even useful, mind the 'git branch'</remarks>
        /// <returns>list of all branches</returns>
        public IReadOnlyList<Branch> GetBranches()
        {
            RestRequest request = new("/repos/{user}/{repo}/branches");
            request.AddUrlSegment("user", Owner.Login);
            request.AddUrlSegment("repo", Name);

            return _client.GetList<Branch>(request);
        }

        /// <summary>
        /// Retrieves the name of the default branch
        /// </summary>
        /// <returns>The name of the default branch</returns>
        public string GetDefaultBranch()
        {
            RestRequest request = new("/repos/{user}/{repo}");
            request.AddUrlSegment("user", Owner.Login);
            request.AddUrlSegment("repo", Name);

            Repository repo = _client.Get<Repository>(request).Data;

            return repo == null ? null : repo.DefaultBranch;
        }

        /// <summary>
        /// Lists all open pull requests
        /// </summary>
        /// <returns>list of all open pull requests</returns>
        public IReadOnlyList<PullRequest> GetPullRequests()
        {
            RestRequest request = new("/repos/{user}/{repo}/pulls");
            request.AddUrlSegment("user", Owner.Login);
            request.AddUrlSegment("repo", Name);

            List<PullRequest> pullRequests = _client.GetList<PullRequest>(request);
            if (pullRequests == null)
                return null;

            pullRequests.ForEach(pr => { pr._client = _client; pr.Repository = this; });
            return pullRequests;
        }

        /// <summary>
        /// Returns a single pull request.
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>the single pull request</returns>
        public PullRequest GetPullRequest(int id)
        {
            RestRequest request = new("/repos/{user}/{repo}/pulls/{pull}");
            request.AddUrlSegment("user", Owner.Login);
            request.AddUrlSegment("repo", Name);
            request.AddUrlSegment("pull", id.ToString());

            PullRequest pullrequest = _client.Get<PullRequest>(request).Data;
            if (pullrequest == null)
                return null;

            pullrequest._client = _client;
            pullrequest.Repository = this;
            return pullrequest;
        }
        /// <summary>
        /// Creates a new pull request
        /// </summary>
        /// <param name="headBranch">branch in the own repository, like mabako:new-awesome-thing</param>
        /// <param name="baseBranch">branch it should be merged into in the original repository, like master</param>
        /// <param name="title">title of the request</param>
        /// <param name="body">body/message</param>
        /// <returns></returns>
        public PullRequest CreatePullRequest(string headBranch, string baseBranch, string title, string body)
        {
            RestRequest request = new("/repos/{name}/{repo}/pulls");
            request.AddUrlSegment("name", Owner.Login);
            request.AddUrlSegment("repo", Name);

            request.RequestFormat = DataFormat.Json;
            request.JsonSerializer = new ReplacingJsonSerializer("\"x__custom__base\":\"", "\"base\":\"");
            request.AddJsonBody(new
            {
                title = title,
                body = body,
                head = headBranch,
                x__custom__base = baseBranch
            });

            PullRequest pullrequest = _client.Post<PullRequest>(request).Data;
            if (pullrequest == null)
                return null;

            pullrequest._client = _client;
            pullrequest.Repository = this;
            return pullrequest;
        }

        public GitHubReference GetRef(string refName)
        {
            RestRequest request = new("/repos/{owner}/{repo}/git/refs/{ref}");
            request.AddUrlSegment("owner", Owner.Login);
            request.AddUrlSegment("repo", Name);
            request.AddUrlSegment("ref", refName);

            GitHubReference ghRef = _client.Get<GitHubReference>(request).Data;
            if (ghRef == null)
                return null;

            ghRef._client = _client;
            ghRef.Repository = this;
            return ghRef;
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="title">title</param>
        /// <param name="body">body</param>
        /// <returns>the issue if successful, null otherwise</returns>
        public Issue CreateIssue(string title, string body)
        {
            RestRequest request = new("/repos/{owner}/{repo}/issues");
            request.AddUrlSegment("owner", Owner.Login);
            request.AddUrlSegment("repo", Name);

            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(new { title, body });

            Issue issue = _client.Post<Issue>(request).Data;
            if (issue == null)
                return null;

            issue._client = _client;
            issue.Repository = this;
            return issue;
        }

        public override bool Equals(object obj)
            => obj is Repository && GetHashCode() == obj.GetHashCode();

        public override int GetHashCode() => GetType().GetHashCode() + ToString().GetHashCode();
        public override string ToString() => Owner.Login + "/" + Name;
    }
}
