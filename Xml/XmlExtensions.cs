using System;
using System.Xml;
using System.Collections.Generic;
using System.Xml.XPath;
using System.IO;

namespace Common.Xml
{
    public static class XmlExtensions
    {
        public static void WriteElementValue(this XmlWriter writer, string elementName, TimeSpan value)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(value.TotalMilliseconds);
            writer.WriteEndElement();
        }

        public static void WriteElementValue(this XmlWriter writer, string elementName, object value)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void ReadElementValue(this XmlReader reader, string elementName, ref string value)
        {
            try
            {
                reader.ReadStartElement(elementName);
                value = reader.ReadContentAsString();
                reader.ReadEndElement();
            }
            catch (XmlException)
            {
                // Ignore
            }
        }

        public static void ReadElementValue(this XmlReader reader, string elementName, ref int value)
        {
            try
            {
                reader.ReadStartElement(elementName);
                value = reader.ReadContentAsInt();
                reader.ReadEndElement();
            }
            catch (XmlException)
            {
                // Ignore
            }
        }

        public static void ReadElementValue(this XmlReader reader, string elementName, ref DateTime value)
        {
            try
            {
                reader.ReadStartElement(elementName);
                value = reader.ReadContentAsDateTime();
                reader.ReadEndElement();
            }
            catch (XmlException)
            {
                // Ignore
            }
        }

        public static void ReadElementValue(this XmlReader reader, string elementName, ref bool value)
        {
            try
            {
                reader.ReadStartElement(elementName);
                value = reader.ReadContentAsBoolean();
                reader.ReadEndElement();
            }
            catch (XmlException)
            {
                // Ignore
            }
        }

        public static void ReadElementValue(this XmlReader reader, string elementName, ref TimeSpan value)
        {
            try
            {
                reader.ReadStartElement(elementName);
                int milliseconds = reader.ReadContentAsInt();
                value = new TimeSpan(0, 0, 0, 0, milliseconds);
                reader.ReadEndElement();
            }
            catch (XmlException)
            {
                // Ignore
            }
        }

        public static XmlNamespaceManager GetAllNamespaces(this XmlDocument document)
        {
            // Create the namespace manager from the name table
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(document.NameTable);

            // Create a dictionary of all namespaces
            Dictionary<string, string> allNamespaces = new Dictionary<string, string>();

            // Create an XPathDocument from the XML of the XmlDocument
            XPathDocument xPathDocument = new XPathDocument(new StringReader(document.InnerXml));

            // Create an XPathNavigator for the document
            XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();

            // Loop over all elements
            while (xPathNavigator.MoveToFollowing(XPathNodeType.Element))
            {
                // Get the list of local namespaces
                var localNamespaces = xPathNavigator.GetNamespacesInScope(XmlNamespaceScope.Local);

                // Add all local namespaces to the master list
                foreach (var ns in localNamespaces)
                    allNamespaces[ns.Key] = ns.Value;
            }

            // Loop over all namespaces
            foreach (var ns in allNamespaces)
            {
                // Use the key as the name
                string namespaceName = ns.Key;

                // If the name is blank then use "default" instead
                if (string.IsNullOrEmpty(namespaceName))
                    namespaceName = "default";

                // Add the namespace to the manager
                namespaceManager.AddNamespace(namespaceName, ns.Value);
            }

            // Add the default namespace if missing
            if (!namespaceManager.HasNamespace("default"))
                namespaceManager.AddNamespace("default", "");

            return namespaceManager;
        }
    }
}
