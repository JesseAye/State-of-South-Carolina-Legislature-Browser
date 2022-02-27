using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
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
			LoadPage();
		}

		private void LoadPage()
		{
			HtmlAgilityPack.HtmlDocument COLHome = ScrapeSite.GetPage(Domain + CodeOfLaws.SubDirectory);

			string XPath = "//div[@id=\"contentsection\"]/a";

			List<Title> TitleNodes = COLHome.DocumentNode.SelectNodes(XPath)
													.Where(a => a.InnerText.StartsWith("Title"))
													.Select(title => new Title() { URL = Domain + title.Attributes["href"].Value, Description = title.NextSibling.InnerText.Substring(3) })
													.ToList();

			foreach (Title title in TitleNodes)
			{
				if (!CodeOfLaws.AddTitle(title))
				{
					throw new Exception($"Unable to CodeOfLaws.AddTitle() with the following information:\n{title.URL}\n{title.Description}");
				}
				
			}
		}
	}
}
