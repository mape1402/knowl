using System.Net;
using Microsoft.AspNetCore.Http;

namespace KnOwl.WolfAuth;

internal sealed class DefaultKnOwlLoginPageRenderer : IKnOwlLoginPageRenderer
{
    public IResult Render(HttpContext context, KnOwlWolfAuthOptions options)
    {
        var challengeUrl = KnOwlWolfAuthUrls.BuildChallengeUrl(context, options);
        var title = WebUtility.HtmlEncode(options.ApplicationName);
        var subtitle = WebUtility.HtmlEncode(options.Subtitle);
        var buttonText = WebUtility.HtmlEncode(options.LoginButtonText);
        var html = $$$"""
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{{{title}}} - Login</title>
    <style>
        :root{color-scheme:light dark;--bg:#f4f6fb;--panel:#fff;--text:#07051d;--muted:#536079;--border:#d8ddec;--primary:#2563eb;--primary-hover:#1d4ed8;--shadow:0 24px 70px rgba(15,23,42,.16)}
        @media (prefers-color-scheme: dark){:root{--bg:#0f172a;--panel:#1e293b;--text:#f8fafc;--muted:#bfdbfe;--border:#334155;--primary:#60a5fa;--primary-hover:#93c5fd;--shadow:0 24px 80px rgba(0,0,0,.34)}}
        *{box-sizing:border-box}
        body{min-height:100vh;margin:0;display:grid;place-items:center;background:var(--bg);color:var(--text);font-family:Inter,ui-sans-serif,system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif}
        main{width:min(420px,calc(100vw - 32px));background:var(--panel);border:1px solid var(--border);border-radius:8px;box-shadow:var(--shadow);padding:28px}
        h1{margin:0;font-size:1.45rem;line-height:1.2}
        p{margin:10px 0 24px;color:var(--muted)}
        a{display:inline-flex;align-items:center;justify-content:center;width:100%;min-height:44px;border-radius:7px;background:var(--primary);color:#fff;text-decoration:none;font-weight:700}
        a:hover{background:var(--primary-hover)}
    </style>
</head>
<body>
    <main>
        <h1>{{{title}}}</h1>
        <p>{{{subtitle}}}</p>
        <a href="{{{WebUtility.HtmlEncode(challengeUrl)}}}">{{{buttonText}}}</a>
    </main>
</body>
</html>
""";

        return Results.Content(html, "text/html");
    }
}
