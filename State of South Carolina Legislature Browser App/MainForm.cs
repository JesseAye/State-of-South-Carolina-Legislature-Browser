using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace State_of_South_Carolina_Legislature_Browser_App
{
	public partial class MainForm : Form
	{
		private CodeOfLaws CodeOfLaws = CodeOfLaws.Instance;
		private const string Domain = "https://www.scstatehouse.gov";

		public MainForm()
		{
			InitializeComponent();
			new Thread(LoadCodeOfLaws).Start();
		}

		/// <summary>
		/// Gathers the <see cref="Title">Titles</see> from the main Code of Laws <see cref="CodeOfLaws.SubDirectory">subdirectory</see>
		/// </summary>
		private void LoadCodeOfLaws()
		{
			HtmlAgilityPack.HtmlDocument COLHome = ScrapeSite.GetPage(Domain + CodeOfLaws.SubDirectory);

			string XPath = "//div[@id=\"contentsection\"]/a";

			List<Title> TitleNodes = COLHome.DocumentNode.SelectNodes(XPath)
													.Where(a => a.InnerText.StartsWith("Title"))
													.Select(title => new Title() { URL = title.Attributes["href"].Value, Description = title.NextSibling.InnerText.Substring(3), NumeralID = Convert.ToUInt32(title.InnerText.Substring(6)) })
													.ToList();

			foreach (Title title in TitleNodes)
			{
				if (!CodeOfLaws.AddTitle(title))
				{
					MessageBox.Show($"Unable to CodeOfLaws.AddTitle() with the following information:\n{title.URL}\n{title.Description}", "Error Adding Code of Laws Title", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			GetFirstArticleSection();
		}

		/// <summary>
		/// This method is used only for testing purposes.<para/>
		/// We get the link to Chapter 1 from inside the Title 1 webpage. Then we will debug to analyze the object of AllContent to figure out how to programmatically convert the HTML data into our objects
		/// </summary>
		private void GetFirstArticleSection()
		{
			string XPathChapter = "//div[@id=\"contentsection\"]/table/tr/td/a[starts-with(@href, '/code')]";
			HtmlAgilityPack.HtmlDocument Title1 = ScrapeSite.GetPage(Domain + CodeOfLaws.Titles[0].URL);

			HtmlNodeCollection AllTitle1Chapters = Title1.DocumentNode.SelectNodes(XPathChapter);
			AllTitle1Chapters.Insert(4, _TEMPORARY_GetChapter7(Title1));
			Chapter Chapter1 = AllTitle1Chapters.Where(a => a.Attributes["href"].Value.Contains("t01c001")).Select(chapter => new Chapter() { URL = chapter.Attributes["href"].Value }).First();

			HtmlAgilityPack.HtmlDocument Chapter1Doc = ScrapeSite.GetPage(Domain + Chapter1.URL);

			HtmlNode AllContent = Chapter1Doc.DocumentNode.SelectSingleNode("//div[@id=\"contentsection\"]");
		}

		/// <summary>
		/// For some reason, HTMLAgilityPack is not wrapping the three td elements inside of a tr element, like all of the other chapters.<para/>
		/// When inspecting in FireFox and Chrome, this issue does not present itself. This is leading me to believe this is an issue with HTMLAgilityPack parsing of the html. Maybe.
		/// </summary>
		/// <param name="doc">The document to get the node information from</param>
		/// <returns>An HtmlNode that can be inserted into a HtmlNodeCollection</returns>
		private HtmlNode _TEMPORARY_GetChapter7(HtmlAgilityPack.HtmlDocument doc)
		{
			string XPath = "//div[@id=\"contentsection\"]/table/td/a[starts-with(@href, '/code')]";

			var newNode = doc.DocumentNode.SelectSingleNode(XPath);
			
			return newNode;
		}
	}
}
