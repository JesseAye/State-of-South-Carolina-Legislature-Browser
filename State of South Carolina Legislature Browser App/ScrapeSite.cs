using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace State_of_South_Carolina_Legislature_Browser_App
{
	public static class ScrapeSite
	{
		public static HtmlDocument GetPage(string uri)
		{
			HtmlWeb PageHTML = new HtmlWeb();

			return PageHTML.Load(uri);
		}
	}
}
