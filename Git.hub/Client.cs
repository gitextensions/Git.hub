using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;

namespace Git.hub
{
    /// <summary>
    /// Git.hub client, start here.
    /// </summary>
    public class Client
    {
        private readonly RestClient _client;

        /// <summary>
        /// Creates a new client instance for github.com
        /// </summary>
        public Client() : this("https://api.github.com") { }

        /// <summary>
        /// Creates a new client instance
        /// </summary>
        /// <param name="apiEndpoint">the host to connect to, e.g. 'https://api.github.com'</param>
        public Client(string apiEndpoint)
        {
            _client = new RestClient(apiEndpoint) { UserAgent = "mabako/Git.hub" };
        }

        /// <summary>
        /// Sets the client to use username and password with GitHub
        /// </summary>
        /// <param name="user">username</param>
        /// <param name="password">password</param>
        public void setCredentials(string user, string password)
        {
            _client.Authenticator =
                user != null && password != null ? new HttpBasicAuthenticator(user, password) : null;
        }

        /// <summary>
        /// Sets the client to use oauth2 with GitHub
        /// </summary>
        /// <param name="token">oauth2-token</param>
        public void setOAuth2Token(string token)
        {
            _client.Authenticator = token != null ? new OAuth2AuthorizationRequestHeaderAuthenticator(token, "bearer") : null;
        }

        /// <summary>
        /// Lists all repositories for the logged in user
        /// </summary>
        /// <returns>list of repositories</returns>
        public IReadOnlyList<Repository> getRepositories()
        {
            if (_client.Authenticator == null)
                throw new ArgumentException("no authentication details");

            RestRequest request = new("/user/repos?type=all");

            List<Repository> repos = _client.GetList<Repository>(request);
            if (repos == null)
                throw new Exception("Bad Credentials");

            repos.ForEach(r => r._client = _client);
            return repos;
        }

        /// <summary>
        /// Lists all repositories for a particular user
        /// </summary>
        /// <param name="username">username</param>
        /// <returns>list of repositories</returns>
        public IReadOnlyList<Repository> getRepositories(string username)
        {
            IRestRequest request = new RestRequest("/users/{name}/repos")
                .AddUrlSegment("name", username);

            List<Repository> repos = _client.GetList<Repository>(request);
            if (repos == null)
                throw new InvalidOperationException("User does not exist.");

            repos.ForEach(r => r._client = _client);
            return repos;
        }

        /// <summary>
        /// Fetches a single repository from github.com/username/repositoryName.
        /// </summary>
        /// <param name="username">repository owner</param>
        /// <param name="repositoryName">name of the repository</param>
        /// <returns>fetched repository</returns>
        public Repository getRepository(string username, string repositoryName)
        {
            IRestRequest request = new RestRequest("/repos/{name}/{repo}")
                .AddUrlSegment("name", username)
                .AddUrlSegment("repo", repositoryName);

            Repository repo = DoRequest<Repository>(request);
            if (repo == null)
                return null;

            repo._client = _client;
            repo.Detailed = true;
            return repo;
        }

        /// <summary>
        /// Fetches all repositories of an organization
        /// </summary>
        /// <param name="organization">name of the organization</param>
        /// <returns></returns>
        public IReadOnlyList<Repository> getOrganizationRepositories(string organization)
        {
            IRestRequest request = new RestRequest("/orgs/{org}/repos")
                .AddUrlSegment("org", organization);

            List<Repository> repos = _client.GetList<Repository>(request);

            Organization org = new() { Login = organization };
            repos.ForEach(r => { r._client = _client; r.Organization = org; });
            return repos;
        }

        /// <summary>
        /// Retrieves the current user.
        ///
        /// Requires to be logged in (OAuth/User+Password).
        /// </summary>
        /// <returns>current user</returns>
        public User getCurrentUser()
        {
            if (_client.Authenticator == null)
                throw new ArgumentException("no authentication details");

            RestRequest request = new("/user");

            User user = DoRequest<User>(request, false);

            return user;
        }

        public async Task<User> GetUserAsync(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentException("Empty username", nameof(userName));
            }

            RestRequest request = new($"/users/{userName}");

            IRestResponse<User> user = await _client.ExecuteGetAsync<User>(request);
            return user.Data;
        }

        /// <summary>
        /// Searches through all of Github's repositories, similar to the search on the website itself.
        /// </summary>
        /// <param name="query">what to search for</param>
        /// <returns>(limited) list of matching repositories</returns>
        public IReadOnlyList<Repository> searchRepositories(string query)
        {
            RestRequest request = new("/legacy/repos/search/{query}");
            request.AddUrlSegment("query", query);

            APIv2.RepositoryListV2 repos = DoRequest<APIv2.RepositoryListV2>(request);
            if (repos?.Repositories == null)
            {
                throw new Exception($"Could not search for {query}");
            }

            return repos.Repositories.Select(r => r.ToV3(_client)).ToList();
        }

        /// <summary>
        /// Get the issues assigned to the authenticated user.
        /// <see href="https://docs.github.com/fr/rest/issues/issues?apiVersion=2022-11-28#list-issues-assigned-to-the-authenticated-user">GitHub issue api</see>
        /// </summary>
        /// <param name="state">state of the issues returned. Choice: "open", "closed", "all"</param>
        /// <param name="includePullRequests">true to include pull requests; otherwise, false.</param>
        /// <returns>a list of issues assigned to the authenticated user.</returns>
        public IReadOnlyList<Issue> GetAssignedIssues(string state = "open", bool includePullRequests = false)
        {
            var request = new RestRequest("/issues");
            request.AddQueryParameter("state", state);
            request.AddQueryParameter("filter", "assigned");
            request.AddQueryParameter("pulls", includePullRequests ? "true" : "false");

            List<Issue> issues = _client.GetList<Issue>(request);

            issues?.ForEach(i => i._client = _client);

            return issues;
        }

        private T DoRequest<T>(IRestRequest request, bool throwOnError = true) where T : new()
        {
            IRestResponse<T> response = _client.Get<T>(request);
            if (response.IsSuccessful)
            {
                return response.Data;
            }

            if (!throwOnError)
            {
                return default;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (_client.Authenticator == null)
                {
                    throw new UnauthorizedAccessException("Please configure a GitHub authentication token.");
                }

                throw new UnauthorizedAccessException("The GitHub authentication token provided is not valid.");
            }

            throw new Exception(response.StatusDescription);
        }
    }
}
