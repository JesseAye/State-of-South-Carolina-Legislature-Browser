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

		/// <summary>
		/// Gets the <![CDATA[<div id="contentsection">]]> and cleans it of empty <![CDATA[<br>]]> and <![CDATA[<#text>]]> <see cref="HtmlNode">HtmlNodes</see>
		/// </summary>
		/// <param name="Document">The full page <see cref="HtmlAgilityPack.HtmlDocument">HtmlDocument</see></param> to be cleaned
		/// <returns>A single <see cref="HtmlNode"/> with the <see cref="HtmlNode.ChildNodes">ChildNodes</see> that make up the laws we want to scrape</returns>
		public static HtmlNode CleanContentSection(HtmlAgilityPack.HtmlDocument Document)
		{
			List<HtmlNode> XPathsToRemove = Document.DocumentNode.SelectSingleNode(CodeOfLaws.ContentSectionXPath).ChildNodes
																   .Where(node => node.Name == "br")
																   .Where(node => node.InnerText == ""
																				   && !node.HasAttributes
																				   && !node.HasChildNodes)
																   .ToList();

			XPathsToRemove.AddRange(Document.DocumentNode.SelectSingleNode(CodeOfLaws.ContentSectionXPath).ChildNodes
																.Where(node => node.Name == "#text")
																.Where(node => (node.InnerText == "\r\n" || node.InnerText == "\r\n\r\n")
																				&& !node.HasAttributes
																				&& !node.HasChildNodes)
																.ToList());

			//Get a collection of <br> nodes that contain no text/attributes/child nodes, and remove each one. The collection has to be reversed because starting from the top modifies the index of the <br> as nodes are removed
			foreach (string xpath in XPathsToRemove.OrderByDescending(node => node.Line)
													.Select(node => node.XPath.Replace("#text", "text()")))
			{
				Document.DocumentNode.SelectSingleNode(xpath).Remove();
			}

			return Document.DocumentNode.SelectSingleNode(CodeOfLaws.ContentSectionXPath);
		}
	}
}
