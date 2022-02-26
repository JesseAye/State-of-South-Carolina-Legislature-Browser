using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace State_of_South_Carolina_Legislature_Browser_App
{
	/// <summary>
	/// <see href="https://csharpindepth.com/articles/singleton">Creating singleton class structure</see>, uses Fourth version
	/// </summary>
	public class CodeOfLaws
	{
		private const string URL = "https://www.scstatehouse.gov/code/statmast.php";

		public List<Title> Titles { get; } = new List<Title>();

		private static readonly CodeOfLaws _instance = new CodeOfLaws();

		static CodeOfLaws() { }

		private CodeOfLaws() { }

		public static CodeOfLaws Instance
		{
			get
			{
				return _instance;
			}
		}

		public static bool AddTitle(string url)
		{
			return true;
		}
	}

	public class Title
	{
		public string URL { get; set; }

		public string Description { get; set; }

		public List<Chapter> Chapters { get; } = new List<Chapter>();

	}

	public class Chapter
	{
		public string URL { get; set; }

		public string Description { get; set; }

		public List<Article> Articles { get; } = new List<Article>();
	}

	public class Article
	{
		public string URL { get; set; }

		public string Description { get; set; }

		public List<Section> Sections { get; } = new List<Section>();
	}

	public class Section
	{
		public string URL { get; set; }

		public string Description { get; set; }
	}
}
