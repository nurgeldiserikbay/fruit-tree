using System;

namespace FruitTree
{
    /// <summary>
    /// Paste the public Google Docs "Publish to web" URL here after legal review.
    /// Do not use a private edit link, a PDF or an invented placeholder URL.
    /// </summary>
    public static class FruitTreePrivacyConfig
    {
        public const string PublishedPolicyUrl = "";

        public static bool IsPublishedUrlValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                uri.Scheme == Uri.UriSchemeHttps &&
                string.IsNullOrEmpty(uri.UserInfo) &&
                (uri.Host == "docs.google.com" && uri.AbsolutePath.Contains("/document/d/e/") &&
                 uri.AbsolutePath.Contains("/pub"));
        }
    }
}
