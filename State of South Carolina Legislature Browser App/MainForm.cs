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
			int CodeCommissionerCount = 0;
			int EditorsNoteCount = 0;
			int EffectCount = 0;

			string History = "\r\nHISTORY:";
			string CodeCommissionersNote = "\r\nCode Commissioner's Note";
			string EditorsNote = "\r\nEditor's Note";
			string EffectOfAmendment = "\r\nEffect of Amendment";
			
			/*
			var NodeTypes = AllContent.ChildNodes.GroupBy(node => node.Name)
													.Select(node => node.First());

			var AllNonEmptyBRs = AllContent.ChildNodes
								.Where(node => node.Name == "br")
								.Where(node => node.InnerText != ""
													|| node.HasAttributes
													|| node.HasChildNodes);

			var AllNonEmptyPoundTexts = AllContent.ChildNodes.Where(node => node.Name == "#text")
																.Where(node => node.InnerText != "\r\n"
																					&& node.InnerText != "\r\n\r\n");

			var AllEmptyPoundTexts = AllContent.ChildNodes.Where(node => node.Name == "#text")
																.Where(node => node.HasAttributes
																					|| node.HasChildNodes);

			var AllDivs = AllContent.ChildNodes.Where(node => node.Name == "div"); //All <div> nodes should be Title, Chapter (+ description), and Articles

			var AllSpans = AllContent.ChildNodes.Where(node => node.Name == "span"); //All <span> nodes should be defining a Section

			if (AllNonEmptyBRs.Count() > 0)
			{
				throw new Exception("Not all <br> nodes in the HTML Document are empty. The developer will need to properly handle this exception.");
			}
			*/

			for (int i = 0; i < AllContent.ChildNodes.Count - 1; i++)
			{
				if (AllContent.ChildNodes[i].Name == "br")
				{
					continue;
				}

				else if (AllContent.ChildNodes[i].InnerText == "\r\n"
							|| AllContent.ChildNodes[i].InnerText == "\r\n\r\n")
				{
					continue;
				}

				else if (AllContent.ChildNodes[i].Name == "span")
				{
					if (!AllContent.ChildNodes[i].InnerText.StartsWith("SECTION "))
					{
						throw new Exception($"Could not parse a <span> within the webpage. The InnerText \"{AllContent.ChildNodes[i].InnerText}\" did not begin with Section");
					}

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

				/*
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
				*/

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
						do
						{
							i++;
						} while (AllContent.ChildNodes[i].Name != "div");

						do
						{
							i++;
						} while (AllContent.ChildNodes[i].NextSibling.Name != "div"
									|| AllContent.ChildNodes[i].NextSibling.Name != "span"); //TODO: Pick up here. This Do While does not break during the first Article instance

						continue;

						/*
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
						*/
					}

					else if (AllContent.ChildNodes[i].InnerText.StartsWith("CHAPTER "))
					{
						do
						{
							i++;
						} while (AllContent.ChildNodes[i].Name != "div");

						do
						{
							i++;
						} while (AllContent.ChildNodes[i].NextSibling.Name != "div");

						continue; // We should already have a Chapter object for this chapter, so just move on through the for loop

						/*
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
						*/
					}

					else if (AllContent.ChildNodes[i].InnerText.StartsWith("Title "))
					{
						continue; // We should already have a Title object for this title, so just move on through the for loop
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
