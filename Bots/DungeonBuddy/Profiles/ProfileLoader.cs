using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Bots.DungeonBuddy.Helpers;

namespace Bots.DungeonBuddy.Profiles;

internal static class ProfileLoader
{
	private const BindingFlags SearchFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty;

	private static T GetAttribute<T>(MemberInfo mi) where T : Attribute
	{
		return GetAttributes<T>(mi).FirstOrDefault();
	}

	private static IEnumerable<T> GetAttributes<T>(MemberInfo mi) where T : Attribute
	{
		object[] profileElementAttribs = mi.GetCustomAttributes(typeof(T), false);
		return profileElementAttribs.OfType<T>();
	}

	private static void ParseList(object instance, XElement element, ErrorCollection errors)
	{
		Type listGenType = instance.GetType();
		Type listType = listGenType.GetGenericArguments()[0];
		var listTypeElementAttrib = GetAttribute<ProfileElementAttribute>(listType);
		List<ObsoleteProfileElementAttribute> obsElementAttribs = GetAttributes<ObsoleteProfileElementAttribute>(listType).ToList();

		MethodInfo addMethod = listGenType.GetMethod("Add");
		foreach (XElement subElement in element.Elements())
		{
			object value;
			if (IsPrimitive(listType))
			{
				if (!ParsePrimitive(subElement.Value, listType, out value))
				{
					errors.Add(Error(subElement, ErrorType.Error, "Could not parse element value"));
					continue;
				}
			}
			else
			{
				if (listType.IsAbstract)
				{
					errors.Add(Error(subElement, ErrorType.Error, "Can not create a '{0}' because it is abstract", listType.FullName));
					continue;
				}

				if (listTypeElementAttrib == null && obsElementAttribs.Count <= 0)
				{
					errors.Add(Error(subElement, ErrorType.Error, "Can not create a '{0}' because it is not decorated with a ProfileElementAttribute", listType.FullName));
					continue;
				}

				if (!obsElementAttribs.Any(obsAttr => StringComparer.InvariantCultureIgnoreCase.Equals(subElement.Name.LocalName, obsAttr.ElementName)))
				{
					if (listTypeElementAttrib != null)
					{
						string expectedName = listTypeElementAttrib.ElementName ?? listType.Name;
						if (!StringComparer.InvariantCultureIgnoreCase.Equals(subElement.Name.LocalName, expectedName))
						{
							errors.Add(Error(subElement, ErrorType.Error, "Expected name '{0}' but got name '{1}'", expectedName, subElement.Name.LocalName));
							continue;
						}
					}
					else
					{
						errors.Add(Error(subElement, ErrorType.Error, "Expected name '{0}' but got name '{1}'", obsElementAttribs[0].ElementName, subElement.Name.LocalName));
						continue;
					}
				}

				if (!listType.IsValueType && listType.GetConstructor(Type.EmptyTypes) == null)
				{
					errors.Add(Error(subElement, ErrorType.Error, "Can not create a '{0}' because it does not have a parameterless constructor", listType.FullName));
					continue;
				}

				try
				{
					value = Activator.CreateInstance(listType);
				}
				catch (Exception ex)
				{
					errors.Add(Error(subElement, ErrorType.Error, "Entry of type '{0}' can not be constructed due to exception thrown ({1})", listType.FullName, (ex is TargetInvocationException ? ex.InnerException : ex).Message));
					continue;
				}

				Process(value, subElement, errors);
			}

			addMethod.Invoke(instance, new[] { value });
		}
	}

	private static bool ParseAsSpecial(object instance, XElement element, ErrorCollection errors)
	{
		Type objType = instance.GetType();
		if (objType.IsGenericType)
			objType = objType.GetGenericTypeDefinition();

		if (objType == typeof(List<>))
		{
			ParseList(instance, element, errors);
			return true;
		}

		return false;
	}

	public static Error Error(IXmlLineInfo lineInfo, ErrorType type, string message)
	{
		if (lineInfo.HasLineInfo())
			return new Error(message, type, lineInfo.LineNumber, lineInfo.LinePosition);

		return new Error(message, type);
	}

	public static Error Error(IXmlLineInfo lineInfo, ErrorType type, string format, params object[] args)
	{
		return Error(lineInfo, type, string.Format(format, args));
	}

	private static Type GetMemberType(MemberInfo mi)
	{
		Debug.Assert(mi is FieldInfo || mi is PropertyInfo);

		var fi = mi as FieldInfo;
		if (fi != null)
			return fi.FieldType;

		return ((PropertyInfo)mi).PropertyType;
	}

	private static void SetMember(MemberInfo mi, object instance, object value)
	{
		Debug.Assert(mi is FieldInfo || mi is PropertyInfo);

		var fi = mi as FieldInfo;
		if (fi != null)
			fi.SetValue(instance, value);
		else
			((PropertyInfo)mi).SetValue(instance, value, null);
	}

	private static bool IsPrimitive(Type type)
	{
		return (type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr)) || type == typeof(string) || type.IsEnum;
	}

	private static bool ParsePrimitive(string textValue, Type type, out object value)
	{
		try
		{
			if (type.IsEnum)
				value = Enum.Parse(type, textValue);
			else
				value = Convert.ChangeType(textValue, type, CultureInfo.InvariantCulture);

			return true;
		}
		catch
		{
			value = null;
			return false;
		}
	}

	private static bool ParsePrimitive(MemberInfo member, string textValue, IXmlLineInfo xmlNodeLineInfo, ErrorCollection errors, out object value)
	{
		if (!ParsePrimitive(textValue, GetMemberType(member), out value))
		{
			errors.Add(Error(xmlNodeLineInfo, ErrorType.Error, "Element value could not be parsed"));
			return false;
		}

		ValueRangeAttribute[] valueRanges = GetAttributes<ValueRangeAttribute>(member).ToArray();
		if (valueRanges.Length == 0)
			return true;

		foreach (ValueRangeAttribute valueRange in valueRanges)
			if (RangeContains(valueRange, value))
				return true;

		if (valueRanges.Length == 1)
		{
			if (Equals(valueRanges[0].Max, int.MaxValue))
				errors.Add(Error(xmlNodeLineInfo, ErrorType.Error, "Value '{0}' invalid; must be greater than or equal to {1}", value, valueRanges[0].Min));
			else
				errors.Add(Error(xmlNodeLineInfo, ErrorType.Error, "Value '{0}' invalid; must be between {1} and {2}", value, valueRanges[0].Min, valueRanges[0].Max));
		}
		else
		{
			var sb = new StringBuilder();
			sb.AppendFormat("Value '{0}' invalid; must be between ", value);
			for (int i = 0; i < valueRanges.Length; i++)
			{
				if (i == valueRanges.Length - 1)
					sb.AppendFormat("or {0} and {1}", valueRanges[i].Min, valueRanges[i].Max);
				else
					sb.AppendFormat("{0} and {1}, ", valueRanges[i].Min, valueRanges[i].Max);
			}

			errors.Add(Error(xmlNodeLineInfo, ErrorType.Error, sb.ToString()));
		}

		return false;
	}

	private static bool RangeContains(ValueRangeAttribute attribute, object objValue)
	{
		try
		{
			object min = attribute.Min;
			object max = attribute.Max;
			if (min.GetType() != objValue.GetType())
				min = Convert.ChangeType(min, objValue.GetType());

			if (max.GetType() != objValue.GetType())
				max = Convert.ChangeType(max, objValue.GetType());

			var minComp = min as IComparable;
			var maxComp = max as IComparable;
			var objValueCom = objValue as IComparable;

			Debug.Assert(minComp != null && maxComp != null && objValueCom != null);
			return objValueCom.CompareTo(min) >= 0 && objValueCom.CompareTo(max) <= 0;
		}
		catch
		{
			return true;
		}
	}

	private static void SetDefaultValues(object instance, ErrorCollection errors)
	{
		Type instanceType = instance.GetType();
		foreach (MemberInfo member in instanceType.GetProperties(SearchFlags).OfType<MemberInfo>().Concat(instanceType.GetFields(SearchFlags)))
		{
			DefaultValueAttribute attrib = GetAttribute<DefaultValueAttribute>(member);
			if (attrib == null)
				continue;

			try
			{
				SetMember(member, instance, attrib.Value);
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("Could not set default value '{0}' on member '{1}'", attrib.Value != null ? attrib.Value.ToString() : "(null)", member.DeclaringType.FullName + "." + member.Name), ex);
			}
		}
	}

	private static void CheckInstancePassed(object instance, XElement element, ErrorCollection errors)
	{
		Type instanceType = instance.GetType();
		string name = element.Name.LocalName;

		var profileElementAttrib = GetAttribute<ProfileElementAttribute>(instanceType);
		IEnumerable<ObsoleteProfileElementAttribute> obsoleteProfileElementAttribs = GetAttributes<ObsoleteProfileElementAttribute>(instanceType);
		if (profileElementAttrib == null && !obsoleteProfileElementAttribs.Any())
		{
			throw new ArgumentException("Instance passed should be of a type that is decorated with a ProfileElementAttribute", "instance");
		}

		ObsoleteProfileElementAttribute obsAttr = obsoleteProfileElementAttribs.FirstOrDefault(attr => string.Equals(attr.ElementName, name, StringComparison.InvariantCultureIgnoreCase));

		if (profileElementAttrib != null)
		{
			string expectedName = profileElementAttrib.ElementName ?? instanceType.Name;

			if (!string.Equals(expectedName, name, StringComparison.InvariantCultureIgnoreCase))
			{
				if (obsAttr != null)
				{
					if (!string.IsNullOrEmpty(obsAttr.Message))
						errors.Add(Error(element, ErrorType.Warning, "'{0}' has been marked as obsolete: {1}", obsAttr.ElementName, obsAttr.Message));

					return;
				}

				throw new ArgumentException(string.Format("Expected name '{0}' but got name '{1}'", expectedName, name), "element");
			}

			return;
		}

		if (obsAttr == null)
		{
			throw new ArgumentException(string.Format("Expected name '{0}' but got name '{1}'", obsoleteProfileElementAttribs.First().ElementName, name));
		}

		if (!string.IsNullOrEmpty(obsAttr.Message))
			errors.Add(Error(element, ErrorType.Warning, "'{0}' has been marked as obsolete: {1}", obsAttr.ElementName, obsAttr.Message));
	}

	private static void ProcessInternal(object instance, bool checkInstance, XElement element, ErrorCollection errors)
	{
		var xmlAuto = instance as IXmlAutoProcessed;
		if (xmlAuto != null)
			xmlAuto.Element = element;

		if (ParseAsSpecial(instance, element, errors)) // handles parsing stuff like lists
			return;

		SetDefaultValues(instance, errors);

		if (checkInstance)
			CheckInstancePassed(instance, element, errors);

		foreach (XAttribute attribute in element.Attributes())
		{
			ProcessAttribute(instance, attribute, errors);
		}

		foreach (XElement subElement in element.Elements())
		{
			ProcessElement(instance, subElement, errors);
		}

		if (xmlAuto != null)
			xmlAuto.OnProcessingDone(errors);
	}

	public static void Process(object instance, XElement element, ErrorCollection errors)
	{
		if (instance == null)
			throw new ArgumentNullException("instance");
		if (element == null)
			throw new ArgumentNullException("element");

		ProcessInternal(instance, true, element, errors);
	}

	private static void ProcessElement(object instance, XElement element, ErrorCollection errors)
	{
		Type objType = instance.GetType();
		MemberInfo correspondingMember = FindMemberForElement(objType, element);
		if (correspondingMember == null)
		{
			var xmlAuto = instance as IXmlAutoProcessed;
			if (xmlAuto != null && xmlAuto.HandlesUnknownElements)
				xmlAuto.HandleUnknownElement(element, errors);
			else
				errors.Add(Error(element, ErrorType.Warning, "Unknown element '{0}'", element.Name));

			return;
		}

		object value;
		Type memberType = GetMemberType(correspondingMember);
		if (IsPrimitive(memberType))
		{
			if (!ParsePrimitive(correspondingMember, element.Value, element, errors, out value))
				return;
		}
		else
		{
			if (!memberType.IsAbstract && memberType.GetConstructor(Type.EmptyTypes) != null)
			{
				try
				{
					value = Activator.CreateInstance(memberType);
				}
				catch (Exception ex)
				{
					errors.Add(Error(element, ErrorType.Error, "Entry of type '{0}' can not be constructed due to exception thrown ({1})", memberType.FullName, (ex is TargetInvocationException ? ex.InnerException : ex).Message));
					return;
				}

				ProcessInternal(value, false, element, errors);
			}
			else
			{
				errors.Add(Error(element, ErrorType.Error, "Can not create new '{0}'", memberType.FullName));
				return;
			}
		}

		SetMember(correspondingMember, instance, value);
	}

	private static void ProcessAttribute(object instance, XAttribute attribute, ErrorCollection errors)
	{
		Type objType = instance.GetType();
		MemberInfo correspondingMember = FindMemberForAttribute(objType, attribute);
		if (correspondingMember == null)
		{
			var xmlAuto = instance as IXmlAutoProcessed;
			if (xmlAuto != null && xmlAuto.HandlesUnknownAttributes)
				xmlAuto.HandleUnknownAttribute(attribute, errors);
			else
				errors.Add(Error(attribute, ErrorType.Warning, "Unknown attribute '{0}'", attribute.Name));

			return;
		}

		Type memberType = GetMemberType(correspondingMember);
		if (!IsPrimitive(memberType))
		{
			errors.Add(Error(attribute, ErrorType.Error, "Invalid corresponding member type '{0}' is not a primitive type - this is an error in '{1}'", memberType.FullName, objType.FullName));
			return;
		}

		object value;
		if (ParsePrimitive(correspondingMember, attribute.Value, attribute, errors, out value))
			SetMember(correspondingMember, instance, value);
	}

	private static MemberInfo FindMemberForAttribute(IReflect type, XAttribute attribute)
	{
		MemberInfo foundMember = type.GetProperties(SearchFlags).OfType<MemberInfo>().Concat(type.GetFields(SearchFlags))
			.FirstOrDefault(mi =>
			{
				var attrib = GetAttribute<ElementAttributeAttribute>(mi);
				if (attrib != null && StringComparer.InvariantCultureIgnoreCase.Equals(attribute.Name.LocalName, attrib.AttributeName ?? mi.Name))
					return true;

				return false;
			});

		return foundMember;
	}

	private static MemberInfo FindMemberForElement(IReflect type, XElement element)
	{
		MemberInfo foundMember = type.GetProperties(SearchFlags).OfType<MemberInfo>().Concat(type.GetFields(SearchFlags))
			.FirstOrDefault(mi =>
			{
				var attrib = GetAttribute<ProfileElementAttribute>(mi);
				if (attrib != null && StringComparer.InvariantCultureIgnoreCase.Equals(element.Name.LocalName, attrib.ElementName ?? mi.Name))
					return true;

				foreach (ObsoleteProfileElementAttribute obsProfElmAttr in GetAttributes<ObsoleteProfileElementAttribute>(mi))
				{
					if (StringComparer.InvariantCultureIgnoreCase.Equals(element.Name.LocalName, obsProfElmAttr.ElementName))
					{
						return true;
					}
				}

				return false;
			});

		return foundMember;
	}
}
