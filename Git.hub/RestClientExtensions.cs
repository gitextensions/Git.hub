using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using RestSharp;

namespace Git.hub;

internal static partial class RestClientExtensions
{
    [GeneratedRegex(@"<(?<Link>[^>]*)>; rel=""(?<Rel>\w*)""", RegexOptions.Compiled)]
    private static partial Regex LinkHeaderFormatRegex();
    private static readonly Regex LinkHeaderFormat = LinkHeaderFormatRegex();

    public static List<T> GetList<T>(this IRestClient client, IRestRequest request)
    {
        List<T> result = new();
        while (true)
        {
            IRestResponse<List<T>> pageResponse = client.Get<List<T>>(request);

            if (!pageResponse.IsSuccessful)
            {
                Uri fullUrl = client.BuildUri(request);
                Trace.WriteLine($"GitHub request error ({fullUrl}): {pageResponse.StatusCode:D} ({pageResponse.StatusCode:G}) - {pageResponse.StatusDescription}");
                return null;
            }

            if (pageResponse.Data == null)
                return null;

            result.AddRange(pageResponse.Data);

#pragma warning disable CS0618 // Type or member is obsolete
            Parameter linkHeader = pageResponse.Headers.FirstOrDefault(i => string.Equals(i.Name, "Link", StringComparison.OrdinalIgnoreCase));
#pragma warning restore CS0618 // Type or member is obsolete
            if (linkHeader == null)
                break;

            bool hasNext = false;
            foreach (Match match in LinkHeaderFormat.Matches(linkHeader.Value.ToString()))
            {
                if (string.Equals(match.Groups["Rel"].Value, "next", StringComparison.OrdinalIgnoreCase))
                {
                    request = new RestRequest(new Uri(match.Groups["Link"].Value));
                    hasNext = true;
                    break;
                }
            }

            if (!hasNext)
                break;
        }

        return result;
    }
}
