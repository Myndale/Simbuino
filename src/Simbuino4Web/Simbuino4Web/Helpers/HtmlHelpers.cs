using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Routing;

namespace Simbuino4Web.Helpers
{
	// html helper for timestamped .js and .css files
	// see http://evolpin.wordpress.com/2011/03/05/overriding-browser-caching-with-asp-net-mvc/
	public static class HtmlHelpers
	{
		public static HtmlString Script(this UrlHelper helper, string contentPath)
		{
			return new HtmlString(string.Format("<script type='text/javascript' src='{0}'></script>", LatestContent(helper, contentPath)));
		}

		public static string LatestContent(this UrlHelper helper, string contentPath)
		{
			string file = HttpContext.Current.Server.MapPath(contentPath);
			if (File.Exists(file))
			{
				var dateTime = File.GetLastWriteTime(file);
				contentPath = string.Format("{0}?v={1}", contentPath, dateTime.Ticks);
			}
			return helper.Content(contentPath);
		}

		public static HtmlString Css(this UrlHelper helper, string contentPath)
		{
			return new HtmlString(string.Format("<link rel='stylesheet' type='text/css' href='{0}' media='screen' />", LatestContent(helper, contentPath)));
		}

		public static HtmlString SimpleLink(this HtmlHelper html, string url, string text)
		{
			return new HtmlString(string.Format("<a href=\"{0}\">{1}</a>", url, text));
		}

		public static MvcHtmlString SimpleLink(this HtmlHelper html, string url, string text, object htmlAttributes)
		{
			TagBuilder tb = new TagBuilder("a");
			tb.InnerHtml = text;
			tb.MergeAttributes(new RouteValueDictionary(htmlAttributes));
			tb.MergeAttribute("href", url);
			return MvcHtmlString.Create(tb.ToString(TagRenderMode.Normal));
		}

	}
}