﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Project;

namespace ICSharpCode.SharpDevelop.Templates
{
	internal class FileDescriptionTemplate
	{
		string name;
		string language;
		
		// Either content or contentData is set, the other is null
		string content;
		FileName binaryFileName;
		
		string itemType;
		Dictionary<string, string> metadata = new Dictionary<string, string>();
		
		public bool IsDependentFile {
			get {
				return metadata.ContainsKey("DependentUpon");
			}
		}
		
		static readonly string[] knownAttributes = {
			"name", "language", "buildAction", "src", "binary"
		};
		
		internal readonly IReadOnlyFileSystem FileSystem;
		
		public FileDescriptionTemplate(XmlElement xml, IReadOnlyFileSystem fileSystem)
		{
			TemplateLoadException.AssertAttributeExists(xml, "name");
			
			this.FileSystem = fileSystem;
			name = xml.GetAttribute("name");
			language = xml.GetAttribute("language");
			if (xml.HasAttribute("buildAction")) {
				itemType = xml.GetAttribute("buildAction");
			}
			foreach (XmlAttribute attribute in xml.Attributes) {
				string attributeName = attribute.Name;
				if (!knownAttributes.Contains(attributeName)) {
					if (attributeName == "copyToOutputDirectory") {
						ProjectTemplateImpl.WarnObsoleteAttribute(xml, attributeName, "Use upper-case attribute names for MSBuild metadata values!");
						attributeName = "CopyToOutputDirectory";
					}
					if (attributeName == "dependentUpon") {
						ProjectTemplateImpl.WarnObsoleteAttribute(xml, attributeName, "Use upper-case attribute names for MSBuild metadata values!");
						attributeName = "DependentUpon";
					}
					if (attributeName == "subType") {
						ProjectTemplateImpl.WarnObsoleteAttribute(xml, attributeName, "Use upper-case attribute names for MSBuild metadata values!");
						attributeName = "SubType";
					}
					
					metadata[attributeName] = attribute.Value;
				}
			}
			if (xml.HasAttribute("src")) {
				FileName fileName = FileName.Create(StringParser.Parse(xml.GetAttribute("src")));
				try {
					if (xml.HasAttribute("binary") && bool.Parse(xml.GetAttribute("binary"))) {
						binaryFileName = fileName;
					} else {
						content = fileSystem.ReadAllText(fileName);
					}
				} catch (Exception e) {
					content = "Error reading content from " + fileName + ":\n" + e.ToString();
					LoggingService.Warn(content);
				}
			} else {
				content = xml.InnerText;
			}
		}
		
		/// <summary>
		/// Sets meta data properties.
		/// </summary>
		/// <returns>Returns <c>true</c> when the projectItem was changed
		/// (in ItemType or MetaData)</returns>
		public bool SetProjectItemProperties(ProjectItem projectItem)
		{
			if (projectItem == null)
				throw new ArgumentNullException("projectItem");
			if (itemType != null) {
				projectItem.ItemType = new ItemType(itemType);
			}
			foreach (KeyValuePair<string, string> pair in metadata) {
				projectItem.SetMetadata(pair.Key, StringParser.Parse(pair.Value));
			}
			return itemType != null || metadata.Count > 0;
		}
		
		public FileDescriptionTemplate(string name, string language, string content)
		{
			this.name    = name;
			this.language = language;
			this.content  = content;
		}
		
		public string Name {
			get {
				return name;
			}
		}
		
		public string Language {
			get {
				return language;
			}
		}
		
		public string Content {
			get {
				return content;
			}
		}
		
		public FileName BinaryFileName {
			get {
				return binaryFileName;
			}
		}
	}
}