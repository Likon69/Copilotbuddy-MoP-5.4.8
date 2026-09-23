using System.Xml.Linq;
using Bots.DungeonBuddy.Helpers;

namespace Bots.DungeonBuddy.Profiles;

public interface IXmlAutoProcessed
{
	bool HandlesUnknownAttributes { get; }
	bool HandlesUnknownElements { get; }
	void HandleUnknownAttribute(XAttribute attribute, ErrorCollection errors);
	void HandleUnknownElement(XElement element, ErrorCollection errors);
	void OnProcessingDone(ErrorCollection errors);
	XElement Element { set; }
}
