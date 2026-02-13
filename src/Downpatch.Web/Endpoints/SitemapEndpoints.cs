namespace Downpatch.Web.Endpoints
{
    using global::Downpatch.Web.Services;
    using System.Text;
    using System.Xml.Linq;


    public static class SitemapEndpoints
    {
        public static IEndpointRouteBuilder MapSitemap(this IEndpointRouteBuilder app)
        {
            app.MapGet("/sitemap.xml", (
                HttpContext ctx,
                ContentIndex index,
                MarkdownPageService pages) =>
            {
                var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}".TrimEnd('/');

                XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                var urlset = new XElement(ns + "urlset");

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var entry in index.AllEntries.OrderBy(x => x.Slug))
                {
                    if (!entry.Slug.StartsWith("guide/", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var routeSlug = ToRouteSlug(entry.Slug);

                    if (!pages.TryGetRendered(routeSlug, out var page))
                        continue;

                    if (TryGetBool(page.FrontMatter, "noindex", false))
                        continue;

                    var loc = string.IsNullOrWhiteSpace(routeSlug)
                        ? $"{baseUrl}/guide"
                        : $"{baseUrl}/guide/{routeSlug}";

                    if (!seen.Add(loc))
                        continue;

                    var last = page.LastModifiedUtc;

                    urlset.Add(new XElement(ns + "url",
                        new XElement(ns + "loc", loc),
                        new XElement(ns + "lastmod", last.ToString("yyyy-MM-dd"))
                    ));
                }

                var doc = new XDocument(urlset);

                return Results.Text(
                    doc.ToString(SaveOptions.DisableFormatting),
                    "application/xml",
                    Encoding.UTF8);
            });

            return app;
        }

        private static string ToRouteSlug(string fullSlug)
        {
            fullSlug = (fullSlug ?? "").Trim('/').Replace('\\', '/');

            if (fullSlug.StartsWith("guide/", StringComparison.OrdinalIgnoreCase))
                fullSlug = fullSlug["guide/".Length..];

            if (fullSlug.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
                fullSlug = fullSlug[..^"/index".Length];

            return fullSlug.Trim('/');
        }

        private static bool TryGetBool(IReadOnlyDictionary<string, string> fm, string key, bool fallback)
            => fm.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;
    }

}
