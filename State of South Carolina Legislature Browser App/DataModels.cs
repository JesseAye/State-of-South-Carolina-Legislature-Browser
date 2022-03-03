using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace State_of_South_Carolina_Legislature_Browser_App
{
	/// <summary>
	/// Singleton that holds the structure of the Code of Laws<para/>
	/// <see href="https://csharpindepth.com/articles/singleton">Creating singleton class structure</see>, uses Fourth version
	/// </summary>
	public class CodeOfLaws
	{
		public const string SubDirectory = "/code/statmast.php";
		public const string ContentSectionXPath = "//div[@id=\"contentsection\"]";

		/// <summary>
		/// The sub-unit of the Code of Laws
		/// </summary>
		public static List<Title> Titles { get; } = new List<Title>();

		/// <summary>
		/// Create a single static instance of this class to become a singleton
		/// </summary>
		private static readonly CodeOfLaws _instance = new CodeOfLaws();

		/// <summary>
		/// Not entirely too sure why this is required, but I believe it has something to do with how this class is compiled and telling compiler/interpreter that this will use singleton pattern<para/>
		/// <see href="https://csharpindepth.com/articles/BeforeFieldInit">See Article</see>
		/// </summary>
		static CodeOfLaws() { }

		/// <summary>
		/// Private constructor as part of requirement of the <c>beforefieldinit</c> thing for singleton pattern 
		/// </summary>
		private CodeOfLaws() { }

		/// <summary>
		/// Provide's the single instance of this class
		/// </summary>
		public static CodeOfLaws Instance
		{
			get
			{
				return _instance;
			}
		}

		/// <summary>
		/// Add a title to the List of Titles
		/// </summary>
		/// <param name="title">The title to add</param>
		/// <returns>
		/// True if added and doesn't exist<para/>
		/// False if it already exists
		/// </returns>
		public bool AddTitle(Title title)
		{
			if (!Titles.Contains(title))
			{
				Titles.Add(title);
				return true;
			}

			else
			{
				//TODO: After XML Implementation - Add check to see if any Titles shifted/changed name
				return false;
			}
		}
	}

	/// <summary>
	/// Provides an inheritable class that contains the consistent descriptors between all sub-units of law
	/// </summary>
	public abstract class LawSubUnit
	{
		/// <summary>
		/// The URL of this sub-unit
		/// </summary>
		public string URL { get; set; }

		/// <summary>
		/// The description/identifier of the sub-unit
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// The numeral that identifies this specific sub-unit
		/// </summary>
		public uint NumeralID { get; set; }
	}

	/// <summary>
	/// Holds the structure of the Titles in the law
	/// </summary>
	public class Title : LawSubUnit
	{
		/// <summary>
		/// The <see cref="List{Chapter}">List of Chapters</see> within the parent object 
		/// </summary>
		public List<Chapter> Chapters { get; } = new List<Chapter>();
	}

	/// <summary>
	/// Holds the structure of the <see cref="Chapter">Chapters</see> within <see cref="Title">Titles</see>
	/// </summary>
	public class Chapter : LawSubUnit
	{
		/// <summary>
		/// The List of Articles within the parent object
		/// </summary>
		public List<Article> Articles { get; } = new List<Article>();
	}

	/// <summary>
	/// Holds the structure of the <see cref="Article">Articles</see> within <see cref="Chapter">Chapters</see>
	/// </summary>
	public class Article
	{
		/// <summary>
		/// The List of Sections within the parent object
		/// </summary>
		public List<Section> Sections { get; } = new List<Section>();

		/// <summary>
		/// The description/identifier of the sub-unit
		/// </summary>
		public string Description { get; set; }
	}

	/// <summary>
	/// Holds the structure of the <see cref="Section">Sections</see> within <see cref="Article">Articles</see>
	/// </summary>
	public class Section
	{
		public List<string> Paragraphs;
		public string History;
		public string CodeCommissionersNote;
		public string EditorsNote;
		public string EffectOfAmendment;

		/// <summary>
		/// The description/identifier of the sub-unit
		/// </summary>
		public string Description { get; set; }
	}
}
