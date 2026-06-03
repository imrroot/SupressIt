using System;
using System.Globalization;
using System.IO;
using System.Net;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace SupressIt.Helpers
{
    public static class BrowserVideoDocument
    {
        private const string HostName = "supressit-media.local";

        public static void MapMediaFolder(WebView2 webView, string mediaPath)
        {
            var folder = Path.GetDirectoryName(mediaPath);
            if (string.IsNullOrWhiteSpace(folder))
                return;

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                HostName,
                folder,
                CoreWebView2HostResourceAccessKind.Allow);
        }

        public static string Build(string mediaPath, double speed)
        {
            var source = WebUtility.HtmlEncode(CreateMediaUrl(mediaPath));
            var rate = Math.Max(0.1, speed).ToString(CultureInfo.InvariantCulture);

            return string.Concat(
                "<!doctype html>\n",
                "<html>\n",
                "<head>\n",
                "<meta charset=\"utf-8\">\n",
                "<style>\n",
                "html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#000;}\n",
                "video{width:100%;height:100%;object-fit:contain;display:block;background:#000;}\n",
                "</style>\n",
                "</head>\n",
                "<body>\n",
                "<video id=\"v\" src=\"", source, "\" autoplay loop muted playsinline></video>\n",
                "<script>\n",
                "const v=document.getElementById('v');\n",
                "v.playbackRate=", rate, ";\n",
                "v.addEventListener('ended',function(){v.currentTime=0;v.play();});\n",
                "v.play().catch(function(){});\n",
                "</script>\n",
                "</body>\n",
                "</html>\n");
        }

        private static string CreateMediaUrl(string mediaPath)
        {
            var fileName = Path.GetFileName(mediaPath) ?? "";
            return "https://" + HostName + "/" + Uri.EscapeDataString(fileName);
        }
    }
}
