//!CompilerOption:Optimize:On

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Bots.Professionbuddy.BehaviorTree;
using Bots.Professionbuddy.ComponentBase;
using Bots.Professionbuddy.Components;
using Bots.Professionbuddy.Dynamic;
using Component = Bots.Professionbuddy.BehaviorTree.Component;

namespace Bots.Professionbuddy
{
	public class PbProfile
	{
		protected internal PbProfile(PBBranch branch, string xmlPath = "")
		{
			Branch = branch ?? new PBBranch();
			XmlPath = xmlPath ?? "";
		}

		/// <summary>Path to a .xml PB profile</summary>
		public string XmlPath { get; protected set; }

		/// <summary>Gets or sets the branch.</summary>
		public PBBranch Branch { get; protected set; }

		public int Version { get; protected set; }

		public void SaveXml(string file = null)
		{
			var path = file ?? XmlPath;
			ToXml(
				new XElement("PBProfile"),
				ProfessionbuddyBot.Instance.Branch).Save(path);
			XmlPath = path;
		}

		#region Static members

		public static PbProfile EmptyProfile { get { return new PbProfile(null); } }

		private static XElement ToXml(XElement xml, Composite comp)
		{
			foreach (var pbComp in comp.Children.OfType<IPBComponent>())
			{
				var comment = pbComp as CommentAction;
				if (comment != null)
				{
					xml.Add(new XComment(comment.Text));
				}
				else
				{
					var elementAttr =
						(PBXmlElementAttribute)pbComp.GetType()
							.GetCustomAttributes(typeof(PBXmlElementAttribute), true)
							.FirstOrDefault();

					if (elementAttr == null)
					{
						PBLog.Warn("{0} does not have a PBXmlElementAttribute attached", pbComp.GetType().Name);
						continue;
					}

					var eleName = !string.IsNullOrEmpty(elementAttr.Name) ? elementAttr.Name : pbComp.GetType().Name;
					var newElement = new XElement(eleName);

					pbComp.OnProfileSave(newElement);
					List<PropertyInfo> propInfoList =
						pbComp.GetType()
							.GetProperties()
							.Where(p => p.GetCustomAttributes(typeof(PBXmlAttributeAttribute), true).Any())
							.ToList();

					foreach (PropertyInfo propInfo in propInfoList)
					{
						var xmlAttr =
							propInfo.GetCustomAttributes(typeof(PBXmlAttributeAttribute), true)
								.OfType<PBXmlAttributeAttribute>().FirstOrDefault();

						if (xmlAttr == null) continue;

						string attrName = !string.IsNullOrEmpty(xmlAttr.Name)
							? xmlAttr.Name
							: propInfo.Name;

						if (newElement.Attribute(attrName) != null)
							continue;
						string value = "";
						var typeConverterAttr =
							(TypeConverterAttribute)propInfo.GetCustomAttributes(typeof(TypeConverterAttribute), true).FirstOrDefault();
						if (typeConverterAttr != null)
						{
							try
							{
								var typeConverter = (TypeConverter)Activator.CreateInstance(Type.GetType(typeConverterAttr.ConverterTypeName));
								if (typeConverter.CanConvertTo(typeof(string)))
								{
									value =
										(string)
											typeConverter.ConvertTo(null, CultureInfo.InvariantCulture, propInfo.GetValue(pbComp, null), typeof(string));
								}
								else
								{
									PBLog.Warn("The TypeConvert {0} can not convert to string.", typeConverterAttr.ConverterTypeName);
								}
							}
							catch (Exception ex)
							{
								PBLog.Warn("Type conversion for {0}->{1} has failed.\n{2}", comp.GetType().Name, propInfo.Name, ex);
							}
						}
						else
						{
							value = Convert.ToString(propInfo.GetValue(pbComp, null), CultureInfo.InvariantCulture);
						}
						newElement.Add(new XAttribute(attrName, value));
					}
					var composite = pbComp as Composite;
					if (composite != null)
						ToXml(newElement, composite);

					xml.Add(newElement);
				}
			}
			return xml;
		}

		/// <summary>Profile behavior.</summary>
		//public PrioritySelector Branch { get; protected set; }
		public static PbProfile LoadFromFile(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					string extension = Path.GetExtension(path);
					if (extension != null && extension.Equals(".package", StringComparison.InvariantCultureIgnoreCase))
					{
						using (Package zipFile = Package.Open(path, FileMode.Open, FileAccess.Read))
						{
							PackageRelationship packageRelation = zipFile.GetRelationships().FirstOrDefault();
							if (packageRelation == null)
							{
								PBLog.Warn("{0} contains no usable profiles", path);
								return null;
							}
							PackagePart pbProfilePart = zipFile.GetPart(packageRelation.TargetUri);
							path = ExtractPart(pbProfilePart, DynamicCodeCompiler.TempFolder);
							PackageRelationshipCollection pbProfileRelations = pbProfilePart.GetRelationships();
							foreach (PackageRelationship rel in pbProfileRelations)
							{
								PackagePart hbProfilePart = zipFile.GetPart(rel.TargetUri);
								ExtractPart(hbProfilePart, DynamicCodeCompiler.TempFolder);
							}
						}
					}
					var element = XElement.Load(path);
					element.Attribute("version");
					var branch = (PBBranch)FromXml(element, new PBBranch());
					return new PbProfile(branch, path);
				}
				PBLog.Warn("Profile: {0} does not exist", path);
				return null;
			}
			catch (Exception ex)
			{
				PBLog.Warn(ex.ToString());
				return null;
			}
		}

		public static PbProfile LoadFromXml(XElement element, string xmlPath = null)
		{
			if (element == null)
			{
				PBLog.Warn("FromXml: element is null");
				return null;
			}
			try
			{
				var branch = (PBBranch) FromXml(element, new PBBranch());
				return new PbProfile(branch, xmlPath);
			}
			catch (Exception ex)
			{
				PBLog.Warn(ex.ToString());
				return null;
			}
		}

		private static Composite FromXml(XElement xml, Composite comp)
		{
			if (ProfessionbuddyBot.Instance.PBComponents == null)
				return comp;

			var pbComponentTypes = (from pbComponent in ProfessionbuddyBot.Instance.PBComponents
				select new {Type = pbComponent.GetType(), EleAttr = pbComponent.GetType().GetCustomAttribute<PBXmlElementAttribute>()}
				into t
				where t.EleAttr != null
				select t).ToArray();

			foreach (XNode node in xml.Nodes())
			{
				if (node.NodeType == XmlNodeType.Comment)
				{
					comp.AddChild(new CommentAction(((XComment) node).Value));
					continue;
				}

				if (node.NodeType != XmlNodeType.Element)
					continue;

				var element = (XElement) node;
				var elementName = element.Name.ToString();

				var typeInfo =
					pbComponentTypes.FirstOrDefault(
						t => (string.IsNullOrEmpty(t.EleAttr.Name) && t.Type.Name == elementName) || t.EleAttr.Matches(elementName));

				if (typeInfo == null)
					throw new InvalidOperationException(string.Format("Unable to bind XML Element: {0} to a Type", element.Name));


				var pbComp = (IPBComponent) Activator.CreateInstance(typeInfo.Type);
				pbComp.OnProfileLoad(element);

				var pbXmlAttrs = (from pi in typeInfo.Type.GetProperties()
					let attrAttr = pi.GetCustomAttribute<PBXmlAttributeAttribute>()
					where attrAttr != null
					select new {PropInfo = pi, AttrAttr = attrAttr}).ToList();

				Dictionary<string, string> attributes = element.Attributes().ToDictionary(k => k.Name.ToString(), v => v.Value);

				// use legacy X,Y,Z location for backwards compatability
				if (attributes.ContainsKey("X"))
				{
					string location = string.Format("{0},{1},{2}", attributes["X"], attributes["Y"], attributes["Z"]);
					var prop =
						pbXmlAttrs.Where(
							p => (string.IsNullOrEmpty(p.AttrAttr.Name) && p.PropInfo.Name == "Location") || p.AttrAttr.Matches("Location"))
							.Select(p => p.PropInfo)
							.FirstOrDefault();

					if (prop != null)
					{
						prop.SetValue(pbComp, location, null);
						attributes.Remove("X");
						attributes.Remove("Y");
						attributes.Remove("Z");
					}
				}

				foreach (var attr in attributes)
				{
					var propInfo =
						pbXmlAttrs.Where(
							p => (string.IsNullOrEmpty(p.AttrAttr.Name) && p.PropInfo.Name == attr.Key) || p.AttrAttr.Matches(attr.Key))
							.Select(p => p.PropInfo).FirstOrDefault();

					if (propInfo == null)
					{
						PBLog.Log("{0}->{1} appears to be unused", elementName, attr.Key);
						continue;
					}
					// check if there is a type converter attached
					var typeConverterAttr =
						(TypeConverterAttribute) propInfo.GetCustomAttributes(typeof (TypeConverterAttribute), true).FirstOrDefault();
					if (typeConverterAttr != null)
					{
						try
						{
							var typeConverter = (TypeConverter) Activator.CreateInstance(Type.GetType(typeConverterAttr.ConverterTypeName));
							if (typeConverter.CanConvertFrom(typeof (string)))
							{
								propInfo.SetValue(pbComp, typeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, attr.Value), null);
							}
							else
							{
								PBLog.Warn("The Type Converter {0} can not convert from string.", typeConverterAttr.ConverterTypeName);
							}
						}
						catch (Exception ex)
						{
							PBLog.Warn("Type conversion for {0} has failed.\n{1}", typeInfo.Type.Name + attr.Key, ex);
						}
					}
					else
					{
						propInfo.SetValue(
							pbComp,
							propInfo.PropertyType.IsEnum
								? Enum.Parse(propInfo.PropertyType, attr.Value)
								: Convert.ChangeType(attr.Value, propInfo.PropertyType, CultureInfo.InvariantCulture),
							null);
					}
				}
				if (pbComp is Composite)
					FromXml(element, pbComp as Composite);
				comp.AddChild((Component) pbComp);
			}
			return comp;
		}

		internal static void GetHbprofiles(string pbProfilePath, Component comp, List<LoadProfileAction> list)
		{
			var loadProfileAction = comp as LoadProfileAction;
			if (loadProfileAction != null && !string.IsNullOrEmpty(loadProfileAction.Path) &&
				loadProfileAction.ProfileType == LoadProfileType.Honorbuddy)
			{
				list.Add(loadProfileAction);
			}
			var composite = comp as Composite;
			if (composite != null)
			{
				foreach (var c in composite.Children)
				{
					GetHbprofiles(pbProfilePath, c, list);
				}
			}
		}

		#region Package

		private const string PackageRelationshipType = @"http://schemas.microsoft.com/opc/2006/sample/document";

		private const string ResourceRelationshipType = @"http://schemas.microsoft.com/opc/2006/sample/required-resource";

		public static void CreatePackage(string path, string profilePath)
		{
			try
			{
				Uri partUriProfile = PackUriHelper.CreatePartUri(new Uri(Path.GetFileName(profilePath), UriKind.Relative));
				var hbProfiles = new List<LoadProfileAction>();

				GetHbprofiles(profilePath, ProfessionbuddyBot.Instance.Branch, hbProfiles);
				var hbProfileUrls = hbProfiles.ToDictionary(
					l =>
					{
						var pbProfileDirectory = Path.GetDirectoryName(profilePath);
						return Path.Combine(pbProfileDirectory, l.Path);
					},
					l => PackUriHelper.CreatePartUri(new Uri(l.Path, UriKind.Relative)));

				using (Package package = Package.Open(path, FileMode.Create))
				{
					// Add the PB profile
					PackagePart packagePartDocument = package.CreatePart(partUriProfile, "text/xml", CompressionOption.Normal);
					using (var fileStream = new FileStream(profilePath, FileMode.Open, FileAccess.Read))
					{
						CopyStream(fileStream, packagePartDocument.GetStream());
					}
					package.CreateRelationship(packagePartDocument.Uri, TargetMode.Internal, PackageRelationshipType);

					foreach (var kv in hbProfileUrls)
					{
						PackagePart packagePartHbProfile = package.CreatePart(kv.Value, "text/xml", CompressionOption.Normal);

						using (var fileStream = new FileStream(kv.Key, FileMode.Open, FileAccess.Read))
						{
							CopyStream(fileStream, packagePartHbProfile.GetStream());
						}
						packagePartDocument.CreateRelationship(kv.Value, TargetMode.Internal, ResourceRelationshipType);
					}
				}
			}
			catch (Exception ex)
			{
				PBLog.Warn(ex.ToString());
			}
		}

		private static void CopyStream(Stream source, Stream target)
		{
			const int bufSize = 0x1000;
			var buf = new byte[bufSize];
			int bytesRead;
			while ((bytesRead = source.Read(buf, 0, bufSize)) > 0)
				target.Write(buf, 0, bytesRead);
		}

		private static string ExtractPart(PackagePart packagePart, string targetDirectory)
		{
			string packageRelative = Uri.UnescapeDataString(packagePart.Uri.ToString().TrimStart('/'));
			string fullPath = Path.Combine(targetDirectory, packageRelative);
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
			using (var fileStream = new FileStream(fullPath, FileMode.Create))
			{
				CopyStream(packagePart.GetStream(), fileStream);
			}
			return fullPath;
		}

		#endregion

		#endregion
	}

}
