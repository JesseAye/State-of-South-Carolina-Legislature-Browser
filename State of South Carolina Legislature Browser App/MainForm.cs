using HtmlAgilityPack;
using System.Linq;
using System.Windows.Forms;

namespace State_of_South_Carolina_Legislature_Browser_App
{
	public partial class MainForm : Form
	{
		private CodeOfLaws CodeOfLaws = CodeOfLaws.Instance;
		private const string HomeURI = "https://www.scstatehouse.gov";

		public MainForm()
		{
			InitializeComponent();
			LoadPage();
		}

		private void LoadPage()
		{
			HtmlAgilityPack.HtmlDocument COLHome = ScrapeSite.GetPage($"https://www.scstatehouse.gov/code/statmast.php");

			var TitleNodes = COLHome.DocumentNode.SelectNodes("//div[@id=\"contentsection\"]/a")
													.Where(a => a.InnerText.StartsWith("Title"))
													.Select(title => new Title() { URL = HomeURI + title.Attributes["href"].Value, Description = title.NextSibling.InnerText.Substring(3) })
													.ToList();
		}
	}
}
