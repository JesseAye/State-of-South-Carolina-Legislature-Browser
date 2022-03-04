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
			HtmlAgilityPack.HtmlDocument Chapter1Doc = ScrapeSite.GetPage(Domain + "/code/t01c001.php");

			List<HtmlNode> XPathsToRemove = Chapter1Doc.DocumentNode.SelectSingleNode(CodeOfLaws.ContentSectionXPath).ChildNodes
																.Where(node => node.Name == "br")
																.Where(node => node.InnerText == ""
																				&& !node.HasAttributes
																				&& !node.HasChildNodes)
																.ToList();

			XPathsToRemove.AddRange(Chapter1Doc.DocumentNode.SelectSingleNode(CodeOfLaws.ContentSectionXPath).ChildNodes
																.Where(node => node.Name == "#text")
																.Where(node => (node.InnerText == "\r\n" || node.InnerText == "\r\n\r\n")
																				&& !node.HasAttributes
																				&& !node.HasChildNodes)
																.ToList());

			//Get a collection of <br> nodes that contain no text/attributes/child nodes, and remove each one. The collection has to be reversed because starting from the top modifies the index of the <br> as nodes are removed
			foreach (string xpath in XPathsToRemove.OrderByDescending(node => node.Line)
													.Select(node => node.XPath.Replace("#text", "text()")))
			{
				Chapter1Doc.DocumentNode.SelectSingleNode(xpath).Remove();
			}

			HtmlNode AllContent = Chapter1Doc.DocumentNode.SelectSingleNode(CodeOfLaws.ContentSectionXPath);

			int SectionCount = 0;
			int HistoryCount = 0;
			int CodeCommissionerCount = 0;
			int EditorsNoteCount = 0;
			int EffectCount = 0;

			string History = "\r\nHISTORY:";
			string CodeCommissionersNote = "\r\nCode Commissioner's Note";
			string EditorsNote = "\r\nEditor's Note";
			string EffectOfAmendment = "\r\nEffect of Amendment";

			for (int i = 0; i < AllContent.ChildNodes.Count - 1; i++)
			{
				if (AllContent.ChildNodes[i].Name == "span")
				{
					if (!AllContent.ChildNodes[i].InnerText.StartsWith("SECTION "))
					{
						throw new Exception($"Could not parse a <span> within the webpage. The InnerText \"{AllContent.ChildNodes[i].InnerText}\" did not begin with Section");
					}

					//Section.NumeralID = AllContent.ChildNodes[i].ChildNodes["a"].Attributes["name"].Value;
					SectionCount++;
					i++; //Next line should be the Section description

					if (AllContent.ChildNodes[i].Name == "#text")
					{
						//Section.Description = AllContent.ChildNodes[i].InnerText.Trim();
						i++;
					}

					else
					{
						throw new Exception($"The following ChildNode after {AllContent.ChildNodes[i].PreviousSibling.ChildNodes["#text"].InnerText} does not contain InnerText that describes that section.");
					}

					while (true)
					{
						if (AllContent.ChildNodes[i].InnerText.StartsWith(History)
								|| AllContent.ChildNodes[i].InnerText.StartsWith(CodeCommissionersNote)
								|| AllContent.ChildNodes[i].InnerText.StartsWith(EditorsNote)
								|| AllContent.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment)
								|| AllContent.ChildNodes[i].Name == "span"
								|| AllContent.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (AllContent.ChildNodes[i].Name == "#text")
						{
							//Section.Paragraphs.Add(AllContent.ChildNodes[i].InnerText);
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through a <span> Section.");
						}
					}
				}

				else if (AllContent.ChildNodes[i].InnerText.StartsWith(History))
				{
					//TODO: Process HISTORY: as Section.History
					HistoryCount++;

					while (!AllContent.ChildNodes[i].NextSibling.InnerText.StartsWith(CodeCommissionersNote)
							&& !AllContent.ChildNodes[i].NextSibling.InnerText.StartsWith(EditorsNote)
							&& !AllContent.ChildNodes[i].NextSibling.InnerText.StartsWith(EffectOfAmendment)
							&& AllContent.ChildNodes[i].NextSibling.Name != "span"
							&& AllContent.ChildNodes[i].NextSibling.Name != "div")
					{
						i++;
					}
				}

				else if (AllContent.ChildNodes[i].InnerText.StartsWith(CodeCommissionersNote))
				{
					//TODO: Process Code Commissioner's Note as Section.CodeCommissionersNote
					CodeCommissionerCount++;

					while (!AllContent.ChildNodes[i].NextSibling.InnerText.StartsWith(EditorsNote)
							&& !AllContent.ChildNodes[i].NextSibling.InnerText.StartsWith(EffectOfAmendment)
							&& AllContent.ChildNodes[i].NextSibling.Name != "span"
							&& AllContent.ChildNodes[i].NextSibling.Name != "div")
					{
						i++;
					}
				}

				else if (AllContent.ChildNodes[i].InnerText.StartsWith(EditorsNote))
				{
					//TODO: Process Editor's Notes as Section.EditorsNote
					EditorsNoteCount++;

					while (!AllContent.ChildNodes[i].NextSibling.InnerText.StartsWith(EffectOfAmendment)
								&& AllContent.ChildNodes[i].NextSibling.Name != "span"
								&& AllContent.ChildNodes[i].NextSibling.Name != "div")
					{
						i++;
					}
				}

				else if (AllContent.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment))
				{
					//TODO: Process Editor's Notes as Section.EditorsNote
					EffectCount++;

					while (AllContent.ChildNodes[i].NextSibling.Name != "span"
							&& AllContent.ChildNodes[i].NextSibling.Name != "div")
					{
						i++;
					}
				}

				else if (AllContent.ChildNodes[i].Name == "div")
				{
					if (AllContent.ChildNodes[i].InnerText.StartsWith("ARTICLE "))
					{
						//Article.NumeralID = Convert.ToInt32(AllContent.ChildNodes[i].ChildNodes["#text"].InnerText.Remove(0, 8));
						i++;

						if (AllContent.ChildNodes[i].Name != "div")
						{
							throw new Exception($"The next <div> at {i} after Article is not another <div> that describes the article.");
						}

						//Article.Description = AllContent.ChildNodes[i].InnerText.Trim();

						continue;
					}

					else if (AllContent.ChildNodes[i].InnerText.StartsWith("CHAPTER "))
					{
						i++; //The next line is just the Chapter description, skip it
						continue; //We should already have a Chapter object for this chapter, so just move on through the for loop
					}

					else if (AllContent.ChildNodes[i].InnerText.StartsWith("Title "))
					{
						continue; // We should already have a Title object for this title, so just move on through the for loop
					}

					else
					{
						throw new Exception($"<div> at index {i} is not an Article, Chapter, or Title.");
					}
				}

				else
				{//BP Here and check for any outliers
					//TODO: Handle instances such as t01c001.php AllContent.ChildNodes[331].InnerHTML is "\r\nThe State Insect"
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
