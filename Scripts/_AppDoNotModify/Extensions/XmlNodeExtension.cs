using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Test._ScriptExtensions
{
    public static class XmlNodeExtension
    {
        public static string GetFullDotPath(this XmlNode node)
        {
            string ret = string.Empty;
            if (node != null)
            {
                ret = node.Name;
                if (node.ParentNode != null)
                    ret = node.ParentNode.GetFullDotPath() + "." + ret;
                else
                    ret = node.BaseURI + ret;
            }

            return ret;
        }

        public static string GetText(this XmlNode node)
        {
            string ret = string.Empty;
            if (node.HasChildNodes == true)
            {
                XmlNode child = node.FirstChild;
                if (child.GetType() == typeof(XmlText))
                    ret = child.InnerText;
            }
            return ret;
        }

        public static string GetAttribute(this XmlNode node, string name)
        {
            string ret = string.Empty;
            XmlElement element = node as XmlElement;
            if (element.HasAttribute(name))
                ret = element.GetAttribute(name);
            return ret;
        }

        public static XmlNode GetSingleChildNode(this XmlNode parent, string nodeName)
        {
            XmlNode node = parent.SelectSingleNode(nodeName);
            if (node == null)
                throw new Exception(parent.GetFullDotPath() + " " + nodeName + " not found!");
            return node;
        }

        public static string GetSingleChildNodeText(this XmlNode parent, string nodeName)
        {
            string text = string.Empty;
            XmlNode node = parent.SelectSingleNode(nodeName);
            if (node == null)
                throw new Exception(parent.GetFullDotPath() + " " + nodeName + " not found!");

            text = node.InnerText;
            return text;
        }

        public static XmlNode SearchNodeByAttributeText(this XmlNode parent, string nodeName, string attrName, string attrValue)
        {
            XmlNode node = null;
            XmlElement element = parent as XmlElement;
            if (parent.Name.Equals(nodeName) && element.HasAttribute(attrName) && element.GetAttribute(attrName).Equals(attrValue, StringComparison.OrdinalIgnoreCase))
                node = parent;
            else if (parent.HasChildNodes)
            {
                foreach (XmlNode n in parent.ChildNodes)
                {
                    if (n.NodeType == XmlNodeType.Element)
                    {
                        var e = n as XmlElement;
                        if (n.Name.Equals(nodeName) && e.HasAttribute(attrName) && e.GetAttribute(attrName).Equals(attrValue, StringComparison.OrdinalIgnoreCase))
                            node = n;
                        else if (n.HasChildNodes)
                            node = n.SearchNodeByAttributeText(nodeName, attrName, attrValue);

                        if (node != null)
                            break;
                    }

                }
            }

            return node;
        }

        public static XmlNode SearchNodeContainsAttributeText(this XmlNode parent, string nodeName, string attrName, string attrValue)
        {
            XmlNode node = null;
            XmlElement element = parent as XmlElement;
            if (parent.Name.Equals(nodeName) && element.HasAttribute(attrName) && element.GetAttribute(attrName).IndexOf(attrValue, StringComparison.OrdinalIgnoreCase) >= 0)
                node = parent;
            else if (parent.HasChildNodes)
            {
                foreach (XmlNode n in parent.ChildNodes)
                {
                    if (n.NodeType == XmlNodeType.Element)
                    {
                        var e = n as XmlElement;
                        if (n.Name.Equals(nodeName) && e.HasAttribute(attrName) && e.GetAttribute(attrName).IndexOf(attrValue, StringComparison.OrdinalIgnoreCase) >= 0)
                            node = n;
                        else if (n.HasChildNodes)
                            node = n.SearchNodeContainsAttributeText(nodeName, attrName, attrValue);

                        if (node != null)
                            break;
                    }

                }
            }

            return node;
        }

        public static XmlNode SearchNodeByText(this XmlNode parent, string nodeName, string text)
        {
            XmlNode node = null;

            if (parent.Name.Equals(nodeName) && parent.HasChildNodes && parent.FirstChild.NodeType == XmlNodeType.Text && parent.FirstChild.InnerText.Equals(text, StringComparison.OrdinalIgnoreCase))
                node = parent;
            else if (parent.HasChildNodes)
            {
                foreach (XmlNode n in parent.ChildNodes)
                {
                    if (n.NodeType == XmlNodeType.Element)
                    {
                        if (n.Name.Equals(nodeName) && n.HasChildNodes && n.FirstChild.NodeType == XmlNodeType.Text && n.FirstChild.InnerText.Equals(text, StringComparison.OrdinalIgnoreCase))
                            node = n;
                        else if (n.HasChildNodes)
                            node = n.SearchNodeByText(nodeName, text);

                        if (node != null)
                            break;
                    }

                }
            }

            return node;
        }

        public static XmlNode SearchNodeContainsText(this XmlNode parent, string nodeName, string text)
        {
            XmlNode node = null;

            if (parent.Name.Equals(nodeName) && parent.HasChildNodes && parent.FirstChild.NodeType == XmlNodeType.Text && parent.FirstChild.InnerText.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                node = parent;
            else if (parent.HasChildNodes)
            {
                foreach (XmlNode n in parent.ChildNodes)
                {
                    if (n.NodeType == XmlNodeType.Element)
                    {
                        if (n.Name.Equals(nodeName) && n.HasChildNodes && n.FirstChild.NodeType == XmlNodeType.Text && n.FirstChild.InnerText.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                            node = n;
                        else if (n.HasChildNodes)
                            node = n.SearchNodeContainsText(nodeName, text);

                        if (node != null)
                            break;
                    }

                }
            }

            return node;
        }

        public static XmlNode GetChildByAttributeValue(this XmlNode parent, string nodeName, string attrName, string attrValue)
        {
            XmlNode node = null;

            foreach (XmlElement n in parent.SelectNodes(nodeName))
            {
                if (n.HasAttribute(attrName) && n.GetAttribute(attrName).Equals(attrValue, StringComparison.OrdinalIgnoreCase))
                {
                    node = n;
                    break;
                }
            }
            return node;
        }

        public static XmlNode GetChildByAttributesValue(this XmlNode parent, string nodeName, Dictionary<string, string> attrDict)
        {
            XmlNode node = null;

            foreach (XmlElement n in parent.SelectNodes(nodeName))
            {
                if (attrDict.All(d => n.HasAttribute(d.Key) && n.GetAttribute(d.Key).Equals(d.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    node = n;
                    break;
                }
            }
            return node;
        }
        public static bool SetChildTextByAttributeValue(this XmlNode parent, string nodeName, string attrName, string attrValue, string innterText)
        {
            bool result = false;
            XmlNode node = null;

            foreach (XmlElement n in parent.SelectNodes(nodeName))
            {
                if (n.HasAttribute(attrName) && n.GetAttribute(attrName).Equals(attrValue, StringComparison.OrdinalIgnoreCase))
                {
                    node = n;
                    node.InnerText = innterText;
                    result = true;
                    break;
                }
            }
            return result;
        }


    }
}
