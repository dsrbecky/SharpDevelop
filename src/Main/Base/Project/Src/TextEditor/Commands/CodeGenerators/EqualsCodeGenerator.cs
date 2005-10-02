﻿// <file>
//     <copyright see="prj:///doc/copyright.txt">2002-2005 AlphaSierraPapa</copyright>
//     <license see="prj:///doc/license.txt">GNU General Public License</license>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using ICSharpCode.TextEditor;

using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.Core;

namespace ICSharpCode.SharpDevelop.DefaultEditor.Commands
{
	/// <summary>
	/// Description of EqualsCodeGenerator.	
	/// </summary>
	public class EqualsCodeGenerator : CodeGenerator
	{
		public override string CategoryName {
			get {
				return "Generate Equals and GetHashCode methods";
			}
		}
		
		public override  string Hint {
			get {
				return "no hint";
			}
		}
		
		public override bool IsActive {
			get {
				return currentClass.Fields != null && currentClass.Fields.Count > 0;
			}
		}
		public override int ImageIndex {
			get {
				
				return ClassBrowserIconService.MethodIndex;
			}
		}
		
		public EqualsCodeGenerator(IClass currentClass) : base(currentClass)
		{
		}
		
		protected override void StartGeneration(IList items, string fileExtension)
		{
			editActionHandler.InsertString("public override bool Equals(object obj)");++numOps;
			if (StartCodeBlockInSameLine) {
				editActionHandler.InsertString(" {");++numOps;
			} else {
				Return();
				editActionHandler.InsertString("{");++numOps;
			}
			Return();
			Indent();
			editActionHandler.InsertString("if (!(obj is " + currentClass.Name + ")) return false;");++numOps;
			Return();
			Indent();
			editActionHandler.InsertString("if (this == obj) return true;");++numOps;
			Return();
			string className = "my" + currentClass.Name;
			editActionHandler.InsertString(currentClass.Name + " " + className +  " = (" + currentClass.Name + ")obj;");++numOps;
			Return();
			
			foreach (IField field in currentClass.Fields) {
				Indent();
				IClass cName = field.ReturnType.GetUnderlyingClass();
				if (cName == null || cName.ClassType == ClassType.Struct || cName.ClassType == ClassType.Enum) {
					editActionHandler.InsertString("if (" + field.Name + " != " + className + "." + field.Name + ") return false;");++numOps;
				} else {
					editActionHandler.InsertString("if (" + field.Name + " != null ? "+ field.Name + ".Equals(" + className + "." + field.Name + "): " + className + "." + field.Name + " != null) return false;");++numOps;
				}
				Return();
			}
			
			Return();
			Indent();
			editActionHandler.InsertString("return true");++numOps;
			Return();
			editActionHandler.InsertString("}");++numOps;
			Return();
			Return();
			editActionHandler.InsertString("public virtual int GetHashCode()");++numOps;
			if (StartCodeBlockInSameLine) {
				editActionHandler.InsertString(" {");++numOps;
			} else {
				Return();
				editActionHandler.InsertString("{");++numOps;
			}
			Return();
			Indent();
			editActionHandler.InsertString("return ");++numOps;
			for (int i = 0; i < currentClass.Fields.Count; ++i) {
				IField field = currentClass.Fields[i];
				IClass cName = field.ReturnType.GetUnderlyingClass();
				if (cName == null || cName.ClassType == ClassType.Struct || cName.ClassType == ClassType.Enum) {
					editActionHandler.InsertString(field.Name + ".GetHashCode()");++numOps;
				} else {
					editActionHandler.InsertString("(" + field.Name + " != null ? " + field.Name + ".GetHashCode() : 0)");++numOps;
				}
				if (i + 1 < currentClass.Fields.Count) {
					editActionHandler.InsertString(" ^ ");
				} else {
					editActionHandler.InsertString(";");
				}
				++numOps;
			}
			Return();
			editActionHandler.InsertString("}");++numOps;
			Return();
		}
	}
}
