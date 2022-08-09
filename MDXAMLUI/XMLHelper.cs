using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace MDXAMLUI;

public static class XMLHelper
{
    public static string SanitizeHTML(string htmlText)
    {
        var htmlSW = new StringWriter();
        var htmlDoc = new HtmlAgilityPack.HtmlDocument();

        htmlDoc.LoadHtml(htmlText);
        htmlDoc.OptionOutputAsXml = true;
        htmlDoc.OptionCheckSyntax = true;
        htmlDoc.OptionFixNestedTags = true;
        htmlDoc.Save(htmlSW);
        return htmlSW.ToString();
    }


    public static List<XmlElement> GetElementsByClassName(this XmlElement xmlElement, params string[] classNames)
    {
        var elements = new List<XmlElement>();

        foreach (var childXMLElement in xmlElement.ChildNodes.OfType<XmlElement>())
        {
            if (childXMLElement.GetAttribute("class").Trim().Split(' ').Intersect(classNames).Count() == classNames.Length)
                elements.Add(childXMLElement);
            else
                elements.AddRange(childXMLElement.GetElementsByClassName(classNames));
        }
        return elements;
    }

    public static XmlElement GetNthElement(this XmlElement xmlElement, int index)
    {
        return xmlElement.ChildNodes.OfType<XmlElement>().ToList()[index];
    }

    public static XmlElement GetElementById(this XmlElement xmlElement, string id)
    {
        XmlElement element = null;

        foreach (var childXMLElement in xmlElement.ChildNodes)
        {
            if (childXMLElement.GetType() == typeof(XmlElement)
                && ((XmlElement)childXMLElement).GetAttribute("id").Trim() == id.Trim())
            {
                element = (XmlElement)childXMLElement;
            }
            else
            {
                element = element.GetElementById(id.Trim());
            }
        }
        return element;
    }
}
