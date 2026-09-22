using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using Styx.Common;
using Styx.TreeSharp;
using Styx.WoWInternals;

namespace Styx.CommonBot.Profiles
{
	
	public abstract class CustomForcedBehavior
	{
		
		public T GetAttributeAs<T>(string attributeName, bool isAttributeRequired, CustomForcedBehavior.IConstraintChecker<T> constraints, string[] attributeNameAliases) where T : class
		{
			return (T)((object)this.UtilGetAttributeAs<T>(attributeName, isAttributeRequired, constraints, attributeNameAliases));
		}

		
		public T[] GetAttributeAsArray<T>(string attributeName, bool isAttributeRequired, CustomForcedBehavior.IConstraintChecker<T> constraints, string[] attributeNameAliases, char[] separatorCharacters)
		{
			if (typeof(T) == typeof(WoWPoint))
			{
				return (T[])this.UtilGetAttributeAsWoWPointArray(attributeName, isAttributeRequired, attributeNameAliases);
			}
			constraints = constraints ?? new CustomForcedBehavior.ConstrainTo.Anything<T>();
			
			if (separatorCharacters == null)
			{
				separatorCharacters = new[] { ',', ';', '|' };
			}
			bool flag = false;
			string text = this.UtilLocateKey(isAttributeRequired, attributeName, attributeNameAliases);
			List<T> list = new List<T>();
			if (text != null && this.Args.ContainsKey(text))
			{
				string[] array2 = this.Args[text].Split(separatorCharacters, StringSplitOptions.RemoveEmptyEntries);
				int i = 0;
				while (i < array2.Length)
				{
					string text2 = array2[i];
					T t;
					try
					{
						t = this.UtilConvert<T>(text, text2);
					}
					catch (Exception)
					{
						flag = true;
						goto IL_00D9;
					}
					goto IL_00AD;
					IL_00D9:
					i++;
					continue;
					IL_00AD:
					string text3 = constraints.Check(text, t);
					if (text3 != null)
					{
						this.LogMessage("error", text3, new object[0]);
						flag = true;
						goto IL_00D9;
					}
					list.Add(t);
					goto IL_00D9;
				}
				if (flag)
				{
					list.Clear();
					this.IsAttributeProblem = true;
				}
				return list.ToArray();
			}
			list.Clear();
			return list.ToArray();
		}

		
		public T? GetAttributeAsNullable<T>(string attributeName, bool isAttributeRequired, CustomForcedBehavior.IConstraintChecker<T> constraints, string[] attributeNameAliases) where T : struct
		{
			return (T?)this.UtilGetAttributeAs<T>(attributeName, isAttributeRequired, constraints, attributeNameAliases);
		}

		
		public T[] GetNumberedAttributesAsArray<T>(string baseName, int countRequired, CustomForcedBehavior.IConstraintChecker<T> constraints, string[] aliasBaseNames)
		{
			Func<string, IEnumerable<string>> func = null;
			Func<string, string, Tuple<string, string>> func2 = null;
			Func<Tuple<string, string>, bool> func3 = null;
			Func<Tuple<string, string>, string> func4 = null;
			CustomForcedBehavior.NumberedAttributesClosure<T> @class = new CustomForcedBehavior.NumberedAttributesClosure<T>();
			@class.baseName = baseName;
			@class.parent = this;
			bool flag = false;
			@class.isPoint = typeof(T) == typeof(WoWPoint);
			List<T> list = new List<T>();
			IEnumerable<string> enumerable = this.Args.Keys.Where(new Func<string, bool>(@class.UtilCheckAttribute));
			foreach (string text in enumerable)
			{
				flag |= this.UtilGetNumberedAttribute<T>(text, constraints, list);
			}
			if (aliasBaseNames != null)
			{
				if (func == null)
				{
					func = new Func<string, IEnumerable<string>>(this.UtilGetAliasBaseNames<T>);
				}
				Func<string, IEnumerable<string>> func5 = func;
				if (func2 == null)
				{
					func2 = new Func<string, string, Tuple<string, string>>(CustomForcedBehavior.UtilCreateTuple<T>);
				}
				IEnumerable<Tuple<string, string>> enumerable2 = aliasBaseNames.SelectMany(func5, func2);
				if (func3 == null)
				{
					func3 = new Func<Tuple<string, string>, bool>(@class.UtilCheckAlias);
				}
				IEnumerable<Tuple<string, string>> enumerable3 = enumerable2.Where(func3);
				if (func4 == null)
				{
					func4 = new Func<Tuple<string, string>, string>(CustomForcedBehavior.sUtilGetAttributeAs<T>);
				}
				IEnumerable<string> enumerable4 = enumerable3.Select(func4);
				foreach (string text2 in enumerable4)
				{
					flag |= this.UtilGetNumberedAttribute<T>(text2, constraints, list);
				}
			}
			if (list.Count < countRequired)
			{
				this.LogMessage("error", "The attribute '{0}N' must be provided at least {1} times (saw it '{2}' times).\n(E.g., ButtonText1, ButtonText2, ButtonText3, ...)\nPlease modify the profile to supply {1} attributes with a base name of '{0}'.", new object[] { @class.baseName, countRequired, list.Count });
				flag = true;
			}
			if (flag)
			{
				list.Clear();
				this.IsAttributeProblem = true;
			}
			return list.ToArray();
		}

		
		private bool UtilGetNumberedAttribute<T>(string attr, CustomForcedBehavior.IConstraintChecker<T> constraints, List<T> list_1)
		{
			string text = attr;
			if (typeof(T) == typeof(WoWPoint))
			{
				if (attr.EndsWith("X") || attr.EndsWith("Y") || attr.EndsWith("Z"))
				{
					text = attr.Substring(0, attr.Length - 1);
				}
				if (!attr.EndsWith("X") && this.Args.ContainsKey(text + "X"))
				{
					return false;
				}
			}
			object obj = this.UtilGetAttributeAs<T>(text, false, constraints, null);
			if (obj == null)
			{
				return true;
			}
			list_1.Add((T)((object)obj));
			return false;
		}

		
		private object UtilGetAttributeAs<T>(string attr, bool isRequired, CustomForcedBehavior.IConstraintChecker<T> constraints, string[] aliases)
		{
			Type typeFromHandle = typeof(T);
			if (typeFromHandle == typeof(WoWPoint))
			{
				return this.UtilGetAttributeAsWoWPoint(attr, isRequired, aliases);
			}
			constraints = constraints ?? new CustomForcedBehavior.ConstrainTo.Anything<T>();
			string text = this.UtilLocateKey(isRequired, attr, aliases);
			if (text == null || !this.Args.ContainsKey(text))
			{
				return null;
			}
			string text2 = this.Args[text];
			T t;
			object obj;
			try
			{
				t = this.UtilConvert<T>(text, text2);
				goto IL_0078;
			}
			catch (Exception)
			{
				this.IsAttributeProblem = true;
				obj = null;
			}
			return obj;
			IL_0078:
			string text3 = constraints.Check(text, t);
			if (text3 != null)
			{
				this.LogMessage("error", text3, new object[0]);
				this.IsAttributeProblem = true;
				return null;
			}
			return t;
		}

		private object UtilGetAttributeAsWoWPointArray(string attr, bool isRequired, string[] aliases)
		{
			string key = this.UtilLocateKey(isRequired, attr, aliases);
			var points = new List<WoWPoint>();
			if (key == null || !this.Args.ContainsKey(key))
				return points.ToArray();

			foreach (string value in this.Args[key].Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string[] coordinates = value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
				try
				{
					if (coordinates.Length != 3)
						throw new FormatException();
					points.Add(new WoWPoint(this.UtilConvert<double>(key, coordinates[0]), this.UtilConvert<double>(key, coordinates[1]), this.UtilConvert<double>(key, coordinates[2])));
				}
				catch (Exception)
				{
					this.LogMessage("error", "The '{0}' attribute value '{1}' must contain three coordinates.", new object[] { key, value });
					this.IsAttributeProblem = true;
					return new WoWPoint[0];
				}
			}
			return points.ToArray();
		}

		
		private object UtilGetAttributeAsWoWPoint(string attr, bool isRequired, string[] aliases)
		{
			if (attr == null)
			{
				attr = "";
			}
			string[] array = null;
			string[] array2 = null;
			string[] array3 = null;
			if (aliases != null)
			{
				if (CustomForcedBehavior.Func0 == null)
				{
					CustomForcedBehavior.Func0 = new Func<string, string>(CustomForcedBehavior.UtilAddX);
				}
				array = aliases.Select(CustomForcedBehavior.Func0).ToArray<string>();
				if (CustomForcedBehavior.Func1 == null)
				{
					CustomForcedBehavior.Func1 = new Func<string, string>(CustomForcedBehavior.UtilAddY);
				}
				array2 = aliases.Select(CustomForcedBehavior.Func1).ToArray<string>();
				if (CustomForcedBehavior.Func2 == null)
				{
					CustomForcedBehavior.Func2 = new Func<string, string>(CustomForcedBehavior.UtilAddZ);
				}
				array3 = aliases.Select(CustomForcedBehavior.Func2).ToArray<string>();
			}
			string text = this.UtilLocateKey(false, attr + "X", array);
			string text2 = this.UtilLocateKey(false, attr + "Y", array2);
			string text3 = this.UtilLocateKey(false, attr + "Z", array3);
			string text4;
			if ((text4 = text) == null)
			{
				text4 = text2 ?? text3;
			}
			string text5 = text4;
			if (text5 != null)
			{
				text5 = text5.Substring(0, text5.Length - 1);
				isRequired = true;
			}
			else
			{
				text5 = attr;
			}
			double? num = (double?)this.UtilGetAttributeAs<double>(text5 + "X", isRequired, null, null);
			double? num2 = (double?)this.UtilGetAttributeAs<double>(text5 + "Y", isRequired, null, null);
			double? num3 = (double?)this.UtilGetAttributeAs<double>(text5 + "Z", isRequired, null, null);
			if (num != null && num2 != null && num3 != null)
			{
				return new WoWPoint(num.Value, num2.Value, num3.Value);
			}
			return null;
		}

		
		private bool UtilIsNumberedAttribute(string attr, string aliases, bool isRequired)
		{
			if (!aliases.StartsWith(attr))
			{
				return false;
			}
			string text = aliases.Substring(attr.Length);
			if (isRequired && (text.EndsWith("X") || text.EndsWith("Y") || text.EndsWith("Z")))
			{
				text = text.Substring(0, text.Length - 1);
			}
			int num;
			return text.Length == 0 || int.TryParse(text, out num);
		}

		
		private T UtilConvert<T>(string attr, string aliases)
		{
			Func<string, string> func = null;
			Type typeFromHandle = typeof(T);
			if (typeFromHandle == typeof(bool))
			{
				int num;
				if (int.TryParse(aliases, out num))
				{
					aliases = ((num != 0) ? "true" : "false");
					this.LogMessage("warning", "Attribute's '{0}' value was provided as an integer (saw '{1}')--a boolean was expected.\nThe integral value '{1}' was converted to Boolean({2}).\nPlease update the profile to provide '{2}' for this value.", new object[] { attr, num, aliases });
				}
			}
			else if (typeFromHandle.IsEnum)
			{
				T t = default(T);
				try
				{
					t = (T)((object)Enum.Parse(typeFromHandle, aliases, true));
					if (!Enum.IsDefined(typeFromHandle, t))
					{
						throw new ArgumentException();
					}
					int num2;
					if (int.TryParse(aliases, out num2))
					{
						this.LogMessage("warning", "The '{0}' attribute's value '{1}' has been implicitly converted to the corresponding enumeration '{2}'.\nPlease use the enumeration name '{2}' instead of a number.", new object[]
						{
							attr,
							num2,
							t.ToString()
						});
					}
				}
				catch (Exception)
				{
					string text = "The value '{0}' is not a member of the {1} enumeration.  Allowed values: {2}";
					object obj = aliases;
					object name = typeFromHandle.Name;
					string text2 = ", ";
					IEnumerable<string> names = Enum.GetNames(typeFromHandle);
					if (func == null)
					{
						func = new Func<string, string>(CustomForcedBehavior.sUtilConvert<T>);
					}
					string text3 = string.Format(text, obj, name, string.Join(text2, names.OrderBy(func)));
					this.LogMessage("error", text3, new object[0]);
					throw;
				}
				return t;
			}
			T t2;
			try
			{
				t2 = (T)((object)Convert.ChangeType(aliases, typeFromHandle));
			}
			catch (Exception ex)
			{
				this.LogMessage("error", "The '{0}' attribute's value (saw '{1}') is malformed. ({2})", new object[]
				{
					attr,
					aliases,
					ex.GetType().Name
				});
				throw;
			}
			return t2;
		}

		
		
		
		public bool IsAttributeProblem
		{
			get
			{
				return this.isPoint;
			}
			protected set
			{
				if (value)
				{
					this.isPoint = true;
				}
			}
		}

		
		public void OnStart_HandleAttributeProblem()
		{
			this.UtilReportUnrecognizedAttributes();
			if (this.IsAttributeProblem)
			{
				this.LogMessage("error", "Stopping Honorbuddy.  Please repair the profile!", new object[0]);
				TreeRoot.Stop("Profile has too many errors");
				return;
			}
			if (this.IsDone)
			{
				this.LogMessage("debug", "Behavior sees 'done'.  Skipping behavior.", new object[0]);
			}
		}

		
		public void LogMessage(string messageType, Color? messageColor, string format, params object[] args)
		{
			string text = "";
			if (this.Element != null && ((IXmlLineInfo)this.Element).HasLineInfo())
			{
				text = " @line " + ((IXmlLineInfo)this.Element).LineNumber.ToString();
			}
			if (!string.IsNullOrEmpty(messageType))
			{
				messageType = "(" + messageType.Trim().ToLower() + ")";
			}
			string text2;
			if (messageColor == null && (text2 = messageType) != null)
			{
				if (!(text2 == "(debug)"))
				{
					if (!(text2 == "(error)"))
					{
						if (!(text2 == "(fatal)"))
						{
							if (!(text2 == "(warning)"))
							{
								if (text2 == "(info)")
								{
									messageColor = new Color?(Colors.CornflowerBlue);
								}
							}
							else
							{
								messageColor = new Color?(Colors.DarkOrange);
							}
						}
						else
						{
							messageColor = new Color?(Colors.Red);
						}
					}
					else
					{
						messageColor = new Color?(Colors.Red);
					}
				}
				else
				{
					messageColor = new Color?(Colors.LimeGreen);
				}
			}
			messageColor = new Color?(messageColor ?? Colors.DarkGray);
			string text3 = string.Format("[{0}{1}{2}]: ", this.baseName, messageType, text) + string.Format(format, args);
			if (messageType == "(debug)")
			{
				Styx.Common.Logging.WriteDiagnostic(messageColor.Value, text3);
			}
			else
			{
				Styx.Common.Logging.Write(messageColor.Value, text3);
			}
			if (messageType == "(fatal)")
			{
				Styx.Common.Logging.Write(Colors.Red, "Fatal error. Stopping Honorbuddy.");
				TreeRoot.Stop("Fatal error in custom behavior " + this.baseName + ".");
			}
		}

		
		public void LogMessage(string messageType, string format, params object[] args)
		{
			this.LogMessage(messageType, null, format, args);
		}

		
		public bool UtilIsProgressRequirementsMet(int questId, CustomForcedBehavior.QuestInLogRequirement questInLogRequirement, CustomForcedBehavior.QuestCompleteRequirement questCompleteRequirement)
		{
			if (this.IsAttributeProblem)
			{
				return false;
			}
			if (questId == 0)
			{
				return true;
			}
			Styx.WoWInternals.PlayerQuest questById = StyxWoW.Me.QuestLog.GetQuestById((uint)questId);
			if (questInLogRequirement == CustomForcedBehavior.QuestInLogRequirement.InLog && questById == null)
			{
				return false;
			}
			if (questInLogRequirement == CustomForcedBehavior.QuestInLogRequirement.NotInLog && questById != null)
			{
				return false;
			}
			bool flag = (questById != null && questById.IsCompleted) || StyxWoW.Me.QuestLog.GetCompletedQuests().Contains((uint)questId);
			bool flag2 = questById != null && questById.IsFailed;
			return (questCompleteRequirement != CustomForcedBehavior.QuestCompleteRequirement.Complete || flag) && (questCompleteRequirement != CustomForcedBehavior.QuestCompleteRequirement.CompleteOrFailed || flag || flag2) && (questCompleteRequirement != CustomForcedBehavior.QuestCompleteRequirement.Failed || flag2) && (questCompleteRequirement != CustomForcedBehavior.QuestCompleteRequirement.NotComplete || !flag) && (questCompleteRequirement != CustomForcedBehavior.QuestCompleteRequirement.NotCompleteOrFailed || (!flag && !flag2)) && (questCompleteRequirement != CustomForcedBehavior.QuestCompleteRequirement.NotFailed || !flag2);
		}

		
		private int UtilCountKeyNames(string attr, string[] aliases)
		{
			Func<string, bool> func = null;
			int num = 0;
			if (!string.IsNullOrEmpty(attr))
			{
				num += (this.Args.ContainsKey(attr) ? 1 : 0);
			}
			if (aliases != null)
			{
				int num2 = num;
				if (func == null)
				{
					func = new Func<string, bool>(this.UtilContainsKey);
				}
				num = num2 + aliases.Where(func).Count<string>();
			}
			return num;
		}

		
		private string UtilLocateKey(bool isRequired, string attr, string[] aliases)
		{
			Func<string, bool> func = null;
			this.UtilRecognizeAttributeNames(attr, aliases);
			if (this.UtilCountKeyNames(attr, aliases) > 1)
			{
				List<string> list = new List<string>();
				list.Add(attr);
				list.AddRange(aliases);
				list.Sort();
				this.LogMessage("error", "The attributes [{0}] are aliases for each other, and thus mutually exclusive.\nPlease specify the attribute by its preferred name '{1}'.", new object[]
				{
					"'" + string.Join("', '", list.ToArray()) + "'",
					attr
				});
				this.IsAttributeProblem = true;
				return null;
			}
			if (!string.IsNullOrEmpty(attr) && this.Args.ContainsKey(attr))
			{
				return attr;
			}
			if (aliases != null)
			{
				if (func == null)
				{
					func = new Func<string, bool>(this.UtilContainsNonEmptyKey);
				}
				string text = aliases.Where(func).FirstOrDefault<string>();
				if (!string.IsNullOrEmpty(text))
				{
					this.LogMessage("warning", "Found attribute via its alias name '{0}'.\nPlease update the profile to use its primary name '{1}', instead.", new object[] { text, attr });
					return text;
				}
			}
			if (isRequired)
			{
				this.LogMessage("error", "Attribute '{0}' is required, but was not provided.", new object[] { attr });
				this.IsAttributeProblem = true;
			}
			return null;
		}

		
		private void UtilRecognizeAttributeNames(string attr, string[] aliases)
		{
			if (!string.IsNullOrEmpty(attr) && !this._recognizedAttributes.Contains(attr))
			{
				this._recognizedAttributes.Add(attr);
			}
			if (aliases != null)
			{
				foreach (string text in aliases)
				{
					if (!this._recognizedAttributes.Contains(text))
					{
						this._recognizedAttributes.Add(text);
					}
				}
			}
		}

		
		private void UtilReportUnrecognizedAttributes()
		{
			IEnumerable<string> enumerable = this.Args.Keys.Where(new Func<string, bool>(this.UtilIsUnrecognizedKey));
			if (CustomForcedBehavior.Func3 == null)
			{
				CustomForcedBehavior.Func3 = value => value;
			}
			IOrderedEnumerable<string> orderedEnumerable = enumerable.OrderBy(CustomForcedBehavior.Func3);
			foreach (string text in orderedEnumerable)
			{
				this.LogMessage("warning", "Attribute '{0}' is not recognized by this behavior--ignoring it.", new object[] { text });
			}
		}

		
		
		public virtual string SubversionId
		{
			get
			{
				return "Unknown";
			}
		}

		
		
		public virtual string SubversionRevision
		{
			get
			{
				return "Unknown";
			}
		}

		
		protected CustomForcedBehavior(Dictionary<string, string> args)
		{
			this.dictionary_0 = args;
			this.baseName = string.Format("{0}-v{1}", base.GetType().Name, this.UtilStripSubversionDecorations(this.SubversionRevision));
		}

		
		
		
		public XElement Element { get; set; }

		
		
		public Dictionary<string, string> Args
		{
			get
			{
				return this.dictionary_0;
			}
		}

		
		
		public Styx.TreeSharp.Composite Branch
		{
			get
			{
				Styx.TreeSharp.Composite composite;
				if ((composite = this._branch) == null)
				{
					composite = (this._branch = new Styx.TreeSharp.Decorator(new Styx.TreeSharp.CanRunDecoratorDelegate(this.ExecuteAction), this.CreateBehavior()));
				}
				return composite;
			}
		}

		
		
		public abstract bool IsDone { get; }

		
		protected virtual Styx.TreeSharp.Composite CreateBehavior()
		{
			return new Styx.TreeSharp.PrioritySelector(new Styx.TreeSharp.Composite[0]);
		}

		
		protected virtual bool Execute()
		{
			return true;
		}

		
		public virtual void OnStart()
		{
		}

		
		public virtual void OnTick()
		{
		}

		
		[Obsolete("Use OnFinished instead.")]
		public virtual void Dispose()
		{
		}

		
		public virtual void OnFinished()
		{
		}

		
		private string UtilStripSubversionDecorations(string attr)
		{
			return CustomForcedBehavior.Regex0.Replace(attr, "$1").Trim();
		}

		
		
		private IEnumerable<string> UtilGetAliasBaseNames<T>(string attr)
		{
			return this.Args.Keys;
		}

		
		
		private static Tuple<string, string> UtilCreateTuple<T>(string attr, string aliases)
		{
			return new Tuple<string, string>(attr, aliases);
		}

		
		
		private static string sUtilGetAttributeAs<T>(Tuple<string, string> tuple)
		{
			return tuple.Item1;
		}

		
		
		private static string UtilAddX(string attr)
		{
			return attr + "X";
		}

		
		
		private static string UtilAddY(string attr)
		{
			return attr + "Y";
		}

		
		
		private static string UtilAddZ(string attr)
		{
			return attr + "Z";
		}

		
		
		private static string sUtilConvert<T>(string attr)
		{
			return attr.ToString();
		}

		
		
		private bool UtilContainsKey(string attr)
		{
			return this.Args.ContainsKey(attr);
		}

		
		
		private bool UtilContainsNonEmptyKey(string attr)
		{
			return !string.IsNullOrEmpty(attr) && this.Args.ContainsKey(attr);
		}

		
		
		private bool UtilIsUnrecognizedKey(string attr)
		{
			return !this._recognizedAttributes.Contains(attr);
		}

		
		
		private static string sUtilCountKeyNames(string attr)
		{
			return attr;
		}

		
		
		private bool ExecuteAction(object ctx)
		{
			return this.Execute();
		}

		
		private bool isPoint;

		
		private List<string> _recognizedAttributes = new List<string>();

		
		private readonly Dictionary<string, string> dictionary_0;

		
		private Styx.TreeSharp.Composite _branch;

		
		private readonly string baseName;

		
		private static Regex Regex0 = new Regex("^\\$[^:]+:[:]?[ \t]*([^$]+)[ \t]*\\$$");

		
		
		private XElement xelement_0;

		
		
		private static Func<string, string> Func0;

		
		
		private static Func<string, string> Func1;

		
		
		private static Func<string, string> Func2;

		
		
		private static Func<string, string> Func3;

		
		public static class ConstrainAs
		{
			
			public static CustomForcedBehavior.IConstraintChecker<int> QuestId(CustomForcedBehavior cfb)
			{
				return new CustomForcedBehavior.ConstrainTo.QuestId<int>(cfb);
			}

			
			public static CustomForcedBehavior.IConstraintChecker<int> AuraId = new CustomForcedBehavior.ConstrainTo.Domain<int>(1, int.MaxValue);

			
			public static CustomForcedBehavior.IConstraintChecker<int> CollectionCount = new CustomForcedBehavior.ConstrainTo.Domain<int>(1, 1000);

			
			public static CustomForcedBehavior.IConstraintChecker<int> FactionId = new CustomForcedBehavior.ConstrainTo.Domain<int>(1, int.MaxValue);

			
			public static CustomForcedBehavior.IConstraintChecker<int> HotbarButton = new CustomForcedBehavior.ConstrainTo.Domain<int>(1, 12);

			
			public static CustomForcedBehavior.IConstraintChecker<int> ItemId = new CustomForcedBehavior.ConstrainTo.Domain<int>(1, int.MaxValue);

			
			public static CustomForcedBehavior.IConstraintChecker<int> Milliseconds = new CustomForcedBehavior.ConstrainTo.Domain<int>(0, int.MaxValue);

			
			public static CustomForcedBehavior.IConstraintChecker<int> MobId = new CustomForcedBehavior.ConstrainTo.Domain<int>(1, int.MaxValue);

			
			public static CustomForcedBehavior.IConstraintChecker<int> ObjectId = new CustomForcedBehavior.ConstrainTo.Domain<int>(1, int.MaxValue);

			
			public static CustomForcedBehavior.IConstraintChecker<double> Percent = new CustomForcedBehavior.ConstrainTo.Domain<double>(0.0, 100.0);

			
			public static CustomForcedBehavior.IConstraintChecker<double> Range = new CustomForcedBehavior.ConstrainTo.Domain<double>(1.0, 10000.0);

			
			public static CustomForcedBehavior.IConstraintChecker<int> RepeatCount = new CustomForcedBehavior.ConstrainTo.Domain<int>(1, 1000);

			
			public static CustomForcedBehavior.IConstraintChecker<int> SpellId = new CustomForcedBehavior.ConstrainTo.Domain<int>(1, int.MaxValue);

			
			public static CustomForcedBehavior.IConstraintChecker<string> StringNonEmpty = new CustomForcedBehavior.ConstrainTo.NonEmptyString<string>();

			
			public static CustomForcedBehavior.IConstraintChecker<int> VehicleId = new CustomForcedBehavior.ConstrainTo.Domain<int>(1, int.MaxValue);

			
			public static CustomForcedBehavior.IConstraintChecker<WoWPoint> WoWPointNonEmpty = new CustomForcedBehavior.ConstrainTo.NonEmptyWoWPoint<WoWPoint>();
		}

		
		public abstract class IConstraintChecker<T>
		{
			
			public virtual string Check(string attributeName, T value)
			{
				return null;
			}
		}

		
		public static class ConstrainTo
		{
			
			public class Anything<T> : CustomForcedBehavior.IConstraintChecker<T>
			{
				
				public override string Check(string attributeName, T value)
				{
					return null;
				}
			}

			
			public class Domain<T> : CustomForcedBehavior.IConstraintChecker<T>
			{
				
				public Domain(T minValue, T maxValue)
				{
					this.gparam_0 = maxValue;
					this.val = minValue;
				}

				
				public override string Check(string attributeName, T value)
				{
					bool flag = Comparer<T>.Default.Compare(value, this.gparam_0) > 0;
					if (Comparer<T>.Default.Compare(value, this.val) >= 0 && !flag)
					{
						return null;
					}
					return string.Format("The '{0}' attribute's value (saw '{1}') is not on the closed interval [{2}..{3}].", new object[] { attributeName, value, this.val, this.gparam_0 });
				}

				
				private T gparam_0;

				
				private T val;
			}

			
			public class NonEmptyString<T> : CustomForcedBehavior.IConstraintChecker<string>
			{
				
				public override string Check(string attributeName, string value)
				{
					if (!string.IsNullOrEmpty(value))
					{
						return null;
					}
					return string.Format("The '{0}' attribute's value may not be an empty string (\"\").", attributeName);
				}
			}

			
			public class NonEmptyWoWPoint<T> : CustomForcedBehavior.IConstraintChecker<WoWPoint>
			{
				
				public override string Check(string attributeName, WoWPoint value)
				{
					if (value != WoWPoint.Empty)
					{
						return null;
					}
					return string.Format("The '{0}' attribute's value may not be the empty WoWPoint (e.g., {1}).", attributeName, WoWPoint.Empty);
				}
			}

			
			public class QuestId<T> : CustomForcedBehavior.ConstrainTo.Domain<int>
			{
				
				public QuestId(CustomForcedBehavior cfb)
					: base(1, int.MaxValue)
				{
					this.parent = cfb;
				}

				
				public override string Check(string attributeName, int value)
				{
					if (value == 0)
					{
						this.parent.LogMessage("warning", new Color?(Colors.Red), "The '{0}' attribute's value may not be zero.  For now, we allow it for purposes of backward compatibility; however, it will be ignored.  In a future release, a QuestId of zero will be explicitly disallowed.", new object[] { attributeName });
						return null;
					}
					return base.Check(attributeName, value);
				}

				
				private CustomForcedBehavior parent;
			}

			
			public class SpecificValues<T> : CustomForcedBehavior.IConstraintChecker<T>
			{
				
				public SpecificValues(T[] allowedValues)
				{
					this.gparam_0 = allowedValues;
				}

				
				public override string Check(string attributeName, T value)
				{
					if (this.gparam_0.Contains(value))
					{
						return null;
					}
					Array.Sort<T>(this.gparam_0);
					T[] array = this.gparam_0;
					if (CustomForcedBehavior.ConstrainTo.SpecificValues<T>._converter == null)
					{
						CustomForcedBehavior.ConstrainTo.SpecificValues<T>._converter = new Converter<T, string>(CustomForcedBehavior.ConstrainTo.SpecificValues<T>.UtilConvertToString);
					}
					string[] array2 = Array.ConvertAll<T, string>(array, CustomForcedBehavior.ConstrainTo.SpecificValues<T>._converter);
					return string.Format("The '{0}' attribute's value (saw '{1}') is not one of the allowed values...\n    [{2}].", attributeName, value, "'" + string.Join("', '", array2) + "'");
				}

				
				
				private static string UtilConvertToString(T val)
				{
					return val.ToString();
				}

				
				private T[] gparam_0;

				
				
				private static Converter<T, string> _converter;
			}
		}

		
		public enum QuestCompleteRequirement
		{
			
			DontCare,
			
			Complete,
			
			CompleteOrFailed,
			
			Failed,
			
			NotComplete,
			
			NotCompleteOrFailed,
			
			NotFailed
		}

		
		public enum QuestInLogRequirement
		{
			
			DontCare,
			
			InLog,
			
			NotInLog
		}

		
		
		private sealed class NumberedAttributesClosure<T>
		{
			
			public bool UtilCheckAttribute(string attr)
			{
				return this.parent.UtilIsNumberedAttribute(this.baseName, attr, this.isPoint);
			}

			
			public bool UtilCheckAlias(Tuple<string, string> tuple)
			{
				return this.parent.UtilIsNumberedAttribute(tuple.Item2, tuple.Item1, this.isPoint);
			}

			
			public bool isPoint;

			
			public CustomForcedBehavior parent;

			
			public string baseName;
		}
	}
}
