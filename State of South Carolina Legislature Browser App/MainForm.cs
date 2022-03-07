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
																					NumeralID = title.InnerText.Substring(6) })
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

				if (title.NumeralID == "1")
				{
					AllChapters.Insert(4, _TEMPORARY_GetTitle1Chapter7(TitleDoc));
				}

				//List<Chapter> ChapterNodes = 
			}
		}

		/// <summary>
		/// Get's the Chapter's webpage, cleans up the <![CDATA[<div id="contentsection">]]>, parses the <see cref="HtmlNode">HtmlNodes</see>, and creates a <see cref="Chapter"/> object
		/// </summary>
		private void ParseChapter(string ChapterLink)
		{
			HtmlAgilityPack.HtmlDocument Chapter1Doc = ScrapeSite.GetPage(Domain + ChapterLink);

			HtmlNode ContentSection = ScrapeSite.CleanContentSection(Chapter1Doc);

			int SectionCount = 0;
			int HistoryCount = 0;
			int CodeCommissionerCount = 0;
			int EditorsNoteCount = 0;
			int EffectCount = 0;

			string History = "\r\nHISTORY:";
			string CodeCommissionersNote = "\r\nCode Commissioner's Note";
			string EditorsNote = "\r\nEditor's Note";
			string EffectOfAmendment = "\r\nEffect of Amendment";

			Chapter chapter1 = new Chapter() { NumeralID = "1" };
			string articleIndex = "0";
			string sectionIndex = "0";

			for (int i = 0; i < ContentSection.ChildNodes.Count - 1; i++)
			{
				if (ContentSection.ChildNodes[i].Name == "span")
				{
					Section section = new Section();

					if (!ContentSection.ChildNodes[i].InnerText.StartsWith("SECTION "))
					{
						throw new Exception($"Could not parse a <span> within the webpage. The InnerText \"{ContentSection.ChildNodes[i].InnerText}\" did not begin with Section");
					}

					section.NumeralID = ContentSection.ChildNodes[i].ChildNodes["a"].Attributes["name"].Value.Replace($"1-{chapter1.NumeralID}-", "").Trim('.');
					sectionIndex = section.NumeralID;
					SectionCount++;
					i++; //Next line should be the Section description

					if (ContentSection.ChildNodes[i].Name == "#text")
					{
						section.Description = ContentSection.ChildNodes[i].InnerText.Trim();
						i++;
					}

					else
					{
						throw new Exception($"The following ChildNode after {ContentSection.ChildNodes[i].PreviousSibling.ChildNodes["#text"].InnerText} does not contain InnerText that describes that section.");
					}

					while (true)
					{
						if (ContentSection.ChildNodes[i].InnerText.StartsWith(History)
								|| ContentSection.ChildNodes[i].InnerText.StartsWith(CodeCommissionersNote)
								|| ContentSection.ChildNodes[i].InnerText.StartsWith(EditorsNote)
								|| ContentSection.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment)
								|| ContentSection.ChildNodes[i].Name == "span"
								|| ContentSection.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (ContentSection.ChildNodes[i].Name == "#text")
						{
							section.Paragraphs.Add(ContentSection.ChildNodes[i].InnerText);
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through a <span> Section.");
						}
					}

					chapter1.Articles.Where(article => article.NumeralID == articleIndex.ToString()).First().Sections.Add(section);
				}

				else if (ContentSection.ChildNodes[i].InnerText.StartsWith(History))
				{
					HistoryCount++;

					while (true)
					{
						if (ContentSection.ChildNodes[i].InnerText.StartsWith(CodeCommissionersNote)
							|| ContentSection.ChildNodes[i].InnerText.StartsWith(EditorsNote)
							|| ContentSection.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment)
							|| ContentSection.ChildNodes[i].Name == "span"
							|| ContentSection.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (ContentSection.ChildNodes[i].Name == "#text")
						{
							chapter1.Articles.Where(article => article.NumeralID == articleIndex.ToString()).First()
												.Sections.Where(section => section.NumeralID == sectionIndex).First()
												.History.Add(ContentSection.ChildNodes[i].InnerText.Replace(History, ""));
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through HISTORY: of Section {chapter1.NumeralID}-{articleIndex}-{sectionIndex}");
						}
					}
				}

				else if (ContentSection.ChildNodes[i].InnerText.StartsWith(CodeCommissionersNote))
				{
					CodeCommissionerCount++;
					i++;

					while (true)
					{
						if (ContentSection.ChildNodes[i].InnerText.StartsWith(EditorsNote)
							|| ContentSection.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment)
							|| ContentSection.ChildNodes[i].Name == "span"
							|| ContentSection.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (ContentSection.ChildNodes[i].Name == "#text")
						{
							chapter1.Articles.Where(article => article.NumeralID == articleIndex.ToString()).First()
												.Sections.Where(section => section.NumeralID == sectionIndex).First()
												.CodeCommissionersNote.Add(ContentSection.ChildNodes[i].InnerText);
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through Code Commissioner's Note of Section {chapter1.NumeralID}-{articleIndex}-{sectionIndex}");
						}
					}
				}

				else if (ContentSection.ChildNodes[i].InnerText.StartsWith(EditorsNote))
				{
					EditorsNoteCount++;
					i++;

					while (true)
					{
						if (ContentSection.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment)
							|| ContentSection.ChildNodes[i].Name == "span"
							|| ContentSection.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (ContentSection.ChildNodes[i].Name == "#text")
						{
							chapter1.Articles.Where(article => article.NumeralID == articleIndex.ToString()).First()
												.Sections.Where(section => section.NumeralID == sectionIndex).First()
												.EditorsNote.Add(ContentSection.ChildNodes[i].InnerText);
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through Editor's Note of Section {chapter1.NumeralID}-{articleIndex}-{sectionIndex}");
						}
					}
				}

				else if (ContentSection.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment))
				{
					EffectCount++;
					i++;

					while (true)
					{
						if (ContentSection.ChildNodes[i].Name == "span"
							|| ContentSection.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (ContentSection.ChildNodes[i].Name == "#text")
						{
							chapter1.Articles.Where(article => article.NumeralID == articleIndex.ToString()).First()
												.Sections.Where(section => section.NumeralID == sectionIndex).First()
												.EffectOfAmendment.Add(ContentSection.ChildNodes[i].InnerText);
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through Effect of Amendment of Section {chapter1.NumeralID}-{articleIndex}-{sectionIndex}");
						}
					}
				}

				else if (ContentSection.ChildNodes[i].Name == "div")
				{
					if (ContentSection.ChildNodes[i].InnerText.StartsWith("ARTICLE "))
					{
						Article article = new Article();
						article.NumeralID = ContentSection.ChildNodes[i].ChildNodes["#text"].InnerText.Remove(0, 8);
						articleIndex = article.NumeralID;

						i++;

						if (ContentSection.ChildNodes[i].Name != "div")
						{
							throw new Exception($"The next <div> at {i} after Article is not another <div> that describes the article.");
						}

						article.Description = ContentSection.ChildNodes[i].InnerText.Trim();
						chapter1.Articles.Add(article);

						continue;
					}

					else if (ContentSection.ChildNodes[i].InnerText.StartsWith("CHAPTER "))
					{
						i++; //The next line is just the Chapter description, skip it
						continue; //We should already have a Chapter object for this chapter, so just move on through the for loop
					}

					else if (ContentSection.ChildNodes[i].InnerText.StartsWith("Title "))
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
				 //TODO: Handle instances such as t01c001.php ContentSection.ChildNodes[331].InnerHTML is "\r\nThe State Insect"
				}
			}
		}

		/// <summary>
		/// This method is used only for testing purposes.<para/>
		/// We get the link to Chapter 1 from inside the Title 1 webpage. Then we will debug to analyze the object of AllContent to figure out how to programmatically convert the HTML data into our objects
		/// </summary>
		private void GetFirstArticleSection()
		{
			HtmlAgilityPack.HtmlDocument Chapter1Doc = ScrapeSite.GetPage(Domain + "/code/t01c001.php");

			HtmlNode ContentSection = ScrapeSite.CleanContentSection(Chapter1Doc);

			int SectionCount = 0;
			int HistoryCount = 0;
			int CodeCommissionerCount = 0;
			int EditorsNoteCount = 0;
			int EffectCount = 0;

			string History = "\r\nHISTORY:";
			string CodeCommissionersNote = "\r\nCode Commissioner's Note";
			string EditorsNote = "\r\nEditor's Note";
			string EffectOfAmendment = "\r\nEffect of Amendment";

			Chapter chapter1 = new Chapter() { NumeralID = "1" };
			string articleIndex = "0";
			string sectionIndex = "0";

			for (int i = 0; i < ContentSection.ChildNodes.Count - 1; i++)
			{
				if (ContentSection.ChildNodes[i].Name == "span")
				{
					Section section = new Section();

					if (!ContentSection.ChildNodes[i].InnerText.StartsWith("SECTION "))
					{
						throw new Exception($"Could not parse a <span> within the webpage. The InnerText \"{ContentSection.ChildNodes[i].InnerText}\" did not begin with Section");
					}

					section.NumeralID = ContentSection.ChildNodes[i].ChildNodes["a"].Attributes["name"].Value.Replace($"1-{chapter1.NumeralID}-", "").Trim('.');
					sectionIndex = section.NumeralID;
					SectionCount++;
					i++; //Next line should be the Section description

					if (ContentSection.ChildNodes[i].Name == "#text")
					{
						section.Description = ContentSection.ChildNodes[i].InnerText.Trim();
						i++;
					}

					else
					{
						throw new Exception($"The following ChildNode after {ContentSection.ChildNodes[i].PreviousSibling.ChildNodes["#text"].InnerText} does not contain InnerText that describes that section.");
					}

					while (true)
					{
						if (ContentSection.ChildNodes[i].InnerText.StartsWith(History)
								|| ContentSection.ChildNodes[i].InnerText.StartsWith(CodeCommissionersNote)
								|| ContentSection.ChildNodes[i].InnerText.StartsWith(EditorsNote)
								|| ContentSection.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment)
								|| ContentSection.ChildNodes[i].Name == "span"
								|| ContentSection.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (ContentSection.ChildNodes[i].Name == "#text")
						{
							section.Paragraphs.Add(ContentSection.ChildNodes[i].InnerText);
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through a <span> Section.");
						}
					}

					chapter1.Articles.Where(article => article.NumeralID == articleIndex.ToString()).First().Sections.Add(section);
				}

				else if (ContentSection.ChildNodes[i].InnerText.StartsWith(History))
				{
					HistoryCount++;

					while (true)
					{
						if (ContentSection.ChildNodes[i].InnerText.StartsWith(CodeCommissionersNote)
							|| ContentSection.ChildNodes[i].InnerText.StartsWith(EditorsNote)
							|| ContentSection.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment)
							|| ContentSection.ChildNodes[i].Name == "span"
							|| ContentSection.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (ContentSection.ChildNodes[i].Name == "#text")
						{
							chapter1.Articles.Where(article => article.NumeralID == articleIndex.ToString()).First()
												.Sections.Where(section => section.NumeralID == sectionIndex).First()
												.History.Add(ContentSection.ChildNodes[i].InnerText.Replace(History, ""));
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through HISTORY: of Section {chapter1.NumeralID}-{articleIndex}-{sectionIndex}");
						}
					}
				}

				else if (ContentSection.ChildNodes[i].InnerText.StartsWith(CodeCommissionersNote))
				{
					CodeCommissionerCount++;
					i++;

					while (true)
					{
						if (ContentSection.ChildNodes[i].InnerText.StartsWith(EditorsNote)
							|| ContentSection.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment)
							|| ContentSection.ChildNodes[i].Name == "span"
							|| ContentSection.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (ContentSection.ChildNodes[i].Name == "#text")
						{
							chapter1.Articles.Where(article => article.NumeralID == articleIndex.ToString()).First()
												.Sections.Where(section => section.NumeralID == sectionIndex).First()
												.CodeCommissionersNote.Add(ContentSection.ChildNodes[i].InnerText);
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through Code Commissioner's Note of Section {chapter1.NumeralID}-{articleIndex}-{sectionIndex}");
						}
					}
				}

				else if (ContentSection.ChildNodes[i].InnerText.StartsWith(EditorsNote))
				{
					EditorsNoteCount++;
					i++;

					while (true)
					{
						if (ContentSection.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment)
							|| ContentSection.ChildNodes[i].Name == "span"
							|| ContentSection.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (ContentSection.ChildNodes[i].Name == "#text")
						{
							chapter1.Articles.Where(article => article.NumeralID == articleIndex.ToString()).First()
												.Sections.Where(section => section.NumeralID == sectionIndex).First()
												.EditorsNote.Add(ContentSection.ChildNodes[i].InnerText);
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through Editor's Note of Section {chapter1.NumeralID}-{articleIndex}-{sectionIndex}");
						}
					}
				}

				else if (ContentSection.ChildNodes[i].InnerText.StartsWith(EffectOfAmendment))
				{
					EffectCount++;
					i++;

					while (true)
					{
						if (ContentSection.ChildNodes[i].Name == "span"
							|| ContentSection.ChildNodes[i].Name == "div")
						{
							i--;
							break;
						}

						else if (ContentSection.ChildNodes[i].Name == "#text")
						{
							chapter1.Articles.Where(article => article.NumeralID == articleIndex.ToString()).First()
												.Sections.Where(section => section.NumeralID == sectionIndex).First()
												.EffectOfAmendment.Add(ContentSection.ChildNodes[i].InnerText);
							i++;
						}

						else
						{
							throw new Exception($"Unexpected node while scanning through Effect of Amendment of Section {chapter1.NumeralID}-{articleIndex}-{sectionIndex}");
						}
					}
				}

				else if (ContentSection.ChildNodes[i].Name == "div")
				{
					if (ContentSection.ChildNodes[i].InnerText.StartsWith("ARTICLE "))
					{
						Article article = new Article();
						article.NumeralID = ContentSection.ChildNodes[i].ChildNodes["#text"].InnerText.Remove(0, 8);
						articleIndex = article.NumeralID;

						i++;

						if (ContentSection.ChildNodes[i].Name != "div")
						{
							throw new Exception($"The next <div> at {i} after Article is not another <div> that describes the article.");
						}

						article.Description = ContentSection.ChildNodes[i].InnerText.Trim();
						chapter1.Articles.Add(article);

						continue;
					}

					else if (ContentSection.ChildNodes[i].InnerText.StartsWith("CHAPTER "))
					{
						i++; //The next line is just the Chapter description, skip it
						continue; //We should already have a Chapter object for this chapter, so just move on through the for loop
					}

					else if (ContentSection.ChildNodes[i].InnerText.StartsWith("Title "))
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
					//TODO: Handle instances such as t01c001.php ContentSection.ChildNodes[331].InnerHTML is "\r\nThe State Insect"
				}
			}
		}

		/// <summary>
		/// For some reason, HTMLAgilityPack is not wrapping the three td elements inside of a tr element, like all of the other chapters.<para/>
		/// When inspecting in FireFox and Chrome, this issue does not present itself. This is leading me to believe this is an issue with HTMLAgilityPack parsing of the html. Maybe.
		/// </summary>
		/// <param name="doc">The document to get the node information from</param>
		/// <returns>An HtmlNode that can be inserted into a HtmlNodeCollection</returns>
		private HtmlNode _TEMPORARY_GetTitle1Chapter7(HtmlAgilityPack.HtmlDocument doc)
		{
			string XPath = CodeOfLaws.ContentSectionXPath + "/table/td/a[starts-with(@href, '/code')]";

			var newNode = doc.DocumentNode.SelectSingleNode(XPath);
			
			return newNode;
		}
	}
}
