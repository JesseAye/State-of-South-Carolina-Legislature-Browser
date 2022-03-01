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
			LoadTitles();

			GetFirstArticleSection();
			LoadChapters();
		}

		/// <summary>
		/// Loads titles from the Code of Laws Homepage (<see cref="Domain">Domain</see> + <see cref="CodeOfLaws.SubDirectory">CodeOfLaws.SubDirectory</see>)
		/// </summary>
		private void LoadTitles()
		{
			HtmlAgilityPack.HtmlDocument COLHome = ScrapeSite.GetPage(Domain + CodeOfLaws.SubDirectory);

			string XPath = CodeOfLaws.ContentSectionXPath + "/a";

			List<Title> TitleNodes = COLHome.DocumentNode.SelectNodes(XPath)
													.Where(a => a.InnerText.StartsWith("Title"))
													.Select(title => new Title() {  URL = title.Attributes["href"].Value,
																					Description = title.NextSibling.InnerText.Substring(3),
																					NumeralID = Convert.ToUInt32(title.InnerText.Substring(6)) })
													.ToList();

			foreach (Title title in TitleNodes)
			{
				if (!CodeOfLaws.AddTitle(title))
				{
					MessageBox.Show($"Unable to CodeOfLaws.AddTitle() with the following information:\n{title.URL}\n{title.Description}", "Error Adding Code of Laws Title", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		/// <summary>
		/// Loads Chapters from each Title (<see cref="Domain">Domain</see> + <see cref="Title.Chapters">Titles.Chapters</see>)
		/// </summary>
		private void LoadChapters()
		{
			throw new NotImplementedException();
			string XPathChapter = CodeOfLaws.ContentSectionXPath + "/table/tr/td/a[starts-with(@href, '/code')]";

			foreach (Title title in CodeOfLaws.Titles)
			{
				HtmlAgilityPack.HtmlDocument TitleDoc = ScrapeSite.GetPage(Domain + title.URL);
				HtmlNodeCollection AllChapters = TitleDoc.DocumentNode.SelectNodes(XPathChapter);

				if (title.NumeralID == 0)
				{
					AllChapters.Insert(4, _TEMPORARY_GetChapter7(TitleDoc));
				}

				//List<Chapter> ChapterNodes = 
			}
		}

		/// <summary>
		/// This method is used only for testing purposes.<para/>
		/// We get the link to Chapter 1 from inside the Title 1 webpage. Then we will debug to analyze the object of AllContent to figure out how to programmatically convert the HTML data into our objects
		/// </summary>
		private void GetFirstArticleSection()
		{
			/*
			string XPathChapter = CodeOfLaws.ContentSectionXPath + "/table/tr/td/a[starts-with(@href, '/code')]";
			HtmlAgilityPack.HtmlDocument Title1 = ScrapeSite.GetPage(Domain + CodeOfLaws.Titles[0].URL);

			HtmlNodeCollection AllTitle1Chapters = Title1.DocumentNode.SelectNodes(XPathChapter);
			AllTitle1Chapters.Insert(4, _TEMPORARY_GetChapter7(Title1));
			Chapter Chapter1 = AllTitle1Chapters.Where(a => a.Attributes["href"].Value.Contains("t01c001")).Select(chapter => new Chapter() { URL = chapter.Attributes["href"].Value }).First();
			*/

			HtmlAgilityPack.HtmlDocument Chapter1Doc = ScrapeSite.GetPage(Domain + "/code/t01c001.php");

			HtmlNode AllContent = Chapter1Doc.DocumentNode.SelectSingleNode(CodeOfLaws.ContentSectionXPath);

			int SectionCount = 0;
			int HistoryCount = 0;

			for (int i = 0; i < AllContent.ChildNodes.Count - 1; i++)
			{
				if (AllContent.ChildNodes[i].Name == "br" && AllContent.ChildNodes[i].ChildNodes.Count == 0 && AllContent.ChildNodes[i].Attributes.Count == 0)
				{

				}

				else if (AllContent.ChildNodes[i].InnerText == "\r\n" || AllContent.ChildNodes[i].InnerText == "\r\n\r\n")
				{
					continue;
				}

				else if (AllContent.ChildNodes[i].Name == "span" && AllContent.ChildNodes[i].InnerText.StartsWith("SECTION "))
				{
					if (AllContent.ChildNodes[i].NextSibling.Name == "#text"
							&& AllContent.ChildNodes[i].NextSibling.NextSibling.InnerText == ""
							&& AllContent.ChildNodes[i].NextSibling.NextSibling.NextSibling.InnerText == "") // For performance, skip next sibling since it doesn't need to be processed
					{
						//TODO: Get the 1st sibling's InnerText as an Article (Substring(1) or Trim(), it looks like they typically have a space at the beginning)
						i += 3;
						SectionCount++;
					}

					else
					{
						throw new Exception($"Missing logic after \"Section\" is found for the following InnerText at AllContent.ChildNodes[{i + 1}, {i + 2}, & {i + 3}]:\n" +
							$"{i + 1} - {AllContent.ChildNodes[i + 1].InnerText}\n");
					}
				}

				else if (AllContent.ChildNodes[i].InnerText.StartsWith("\r\n\t")) //Catches (probably) all new paragraphs, with the exception of Editor's Note paragraphs
				{
					if (AllContent.ChildNodes[i].NextSibling.InnerText == ""
							&& AllContent.ChildNodes[i].NextSibling.NextSibling.InnerText == "") // For performance, skip next sibling since it doesn't need to be processed
					{
						//TODO: Get this node's InnerText as a paragraph to the last identified Section
						i += 2;
					}

					else
					{
						throw new Exception($"Missing logic after \"Article\" is found for the following InnerText at AllContent.ChildNodes[{i + 1}{i + 2}{i + 3}]:\n");
					}
				}

				else if (AllContent.ChildNodes[i].InnerText.StartsWith("\r\nHISTORY:"))
				{
					HistoryCount++;
				}

				else if (AllContent.ChildNodes[i].InnerText.StartsWith("\r\nEditor's Note"))
				{

				}

				else if (AllContent.ChildNodes[i].InnerText.StartsWith("ARTICLE "))
				{
					if (AllContent.ChildNodes[i].NextSibling.InnerText == ""
							&& AllContent.ChildNodes[i].NextSibling.NextSibling.InnerText == "\r\n"
							&& AllContent.ChildNodes[i].NextSibling.NextSibling.NextSibling.Name == "div") // For performance, skip next sibling since it doesn't need to be processed
					{
						//TODO: Get the 3rd sibling's InnerText as an Article
						i += 3;
					}

					else
					{
						throw new Exception($"Missing logic after \"Article\" is found for the following InnerText at AllContent.ChildNodes[{i + 1}, {i + 2}, & {i + 3}]:\n" +
							$"{i + 1} - {AllContent.ChildNodes[i + 1].InnerText}\n" +
							$"{i + 2} - {AllContent.ChildNodes[i + 2].InnerText}\n" +
							$"{i + 3} - {AllContent.ChildNodes[i + 3].InnerText}");
					}
				}

				else if (AllContent.ChildNodes[i].InnerText.StartsWith("CHAPTER "))
				{
					if (AllContent.ChildNodes[i].NextSibling.InnerText == ""
							&& AllContent.ChildNodes[i].NextSibling.NextSibling.InnerText == "\r\n"
							&& AllContent.ChildNodes[i].NextSibling.NextSibling.NextSibling.Name == "div") // For performance, skip next 3 siblings since it doesn't need to be processed
					{
						i += 3;
					}

					else
					{
						throw new Exception($"Missing logic after \"Title\" is found for the following InnerText at AllContent.ChildNodes[{i + 1}, {i + 2}, & {i + 3}]:\n" +
							$"{i + 1} - {AllContent.ChildNodes[i + 1].InnerText}\n" +
							$"{i + 2} - {AllContent.ChildNodes[i + 2].InnerText}\n" +
							$"{i + 3} - {AllContent.ChildNodes[i + 3].InnerText}");
					}
				}

				else if (AllContent.ChildNodes[i].InnerText.StartsWith("Title "))
				{
					if (AllContent.ChildNodes[i].NextSibling.InnerText == "\r\n") // For performance, skip next sibling since it doesn't need to be processed
					{
						i++;
					}

					else
					{
						throw new Exception("Missing logic after \"Title\" is found for the following InnerText at AllContent.ChildNodes[" + i + 1 + "]:\n" + AllContent.ChildNodes[i + 1].InnerText);
					}
				}

				else
				{//BP Here and check for any outliers

				}
			}
		}

		/// <summary>
		/// For some reason, HTMLAgilityPack is not wrapping the three td elements inside of a tr element, like all of the other chapters.<para/>
		/// When inspecting in FireFox and Chrome, this issue does not present itself. This is leading me to believe this is an issue with HTMLAgilityPack parsing of the html. Maybe.
		/// </summary>
		/// <param name="doc">The document to get the node information from</param>
		/// <returns>An HtmlNode that can be inserted into a HtmlNodeCollection</returns>
		private HtmlNode _TEMPORARY_GetChapter7(HtmlAgilityPack.HtmlDocument doc)
		{
			string XPath = CodeOfLaws.ContentSectionXPath + "/table/td/a[starts-with(@href, '/code')]";

			var newNode = doc.DocumentNode.SelectSingleNode(XPath);
			
			return newNode;
		}
	}
}
