// created on 04.08.2003 at 17:49
using System;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using RefParser = ICSharpCode.NRefactory.Parser;
using AST = ICSharpCode.NRefactory.Parser.AST;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.Core;

namespace ICSharpCode.SharpDevelop.Dom.NRefactoryResolver
{
	public class Using : AbstractUsing
	{
	}
	
	public class NRefactoryASTConvertVisitor : RefParser.AbstractASTVisitor
	{
		ICompilationUnit cu;
		Stack currentNamespace = new Stack();
		Stack currentClass = new Stack();
		
		public ICompilationUnit Cu {
			get {
				return cu;
			}
		}
		
		public NRefactoryASTConvertVisitor(IProjectContent projectContent)
		{
			cu = new CompilationUnit(projectContent);
		}
		
		// TODO: kill abstract compilation unit, replace with implementation. Maybe the whole Abstract layer ?
		public class CompilationUnit : AbstractCompilationUnit
		{
			public CompilationUnit(IProjectContent projectContent) : base(projectContent)
			{
			}
			
			public override List<IComment> MiscComments {
				get {
					return null;
				}
			}
			public override List<IComment> DokuComments {
				get {
					return null;
				}
			}
		}
		
		public override object Visit(AST.CompilationUnit compilationUnit, object data)
		{
			//TODO: usings, Comments
			if (compilationUnit == null) {
				return null;
			}
			compilationUnit.AcceptChildren(this, data);
			return cu;
		}
		
		public override object Visit(AST.UsingDeclaration usingDeclaration, object data)
		{
			Using us = new Using();
			foreach (AST.Using u in usingDeclaration.Usings) {
				u.AcceptVisitor(this, us);
			}
			cu.Usings.Add(us);
			return data;
		}
		
		public override object Visit(AST.Using u, object data)
		{
			Debug.Assert(data is Using);
			Using us = (Using)data;
			if (u.IsAlias) {
				us.Aliases[u.Alias] = u.Name;
			} else {
				us.Usings.Add(u.Name);
			}
			return data;
		}
		
		List<IAttributeSection> VisitAttributes(ArrayList attributes)
		{
			// TODO Expressions???
			List<IAttributeSection> result = new List<IAttributeSection>();
			foreach (AST.AttributeSection section in attributes) {
				List<IAttribute> resultAttributes = new List<IAttribute>();
				foreach (AST.Attribute attribute in section.Attributes) {
					IAttribute a = new ASTAttribute(attribute.Name, new ArrayList(attribute.PositionalArguments), new SortedList());
					foreach (AST.NamedArgumentExpression n in attribute.NamedArguments) {
						a.NamedArguments[n.Name] = n.Expression;
					}
				}
				AttributeTarget target = AttributeTarget.None;
				if (section.AttributeTarget != null && section.AttributeTarget != "") {
					try {
						target = (AttributeTarget)Enum.Parse(typeof (AttributeTarget), section.AttributeTarget);
					} catch (Exception e) {
						Console.WriteLine("Unexpected AttributeTarget, was " + section.AttributeTarget);
						Console.WriteLine(e);
					}
				}
				IAttributeSection s = new AttributeSection(target, resultAttributes);
				result.Add(s);
			}
			return result;
		}
		
//		ModifierEnum VisitModifier(ICSharpCode.NRefactory.Parser.Modifier m)
//		{
//			return (ModifierEnum)m;
//		}
		
		public override object Visit(AST.NamespaceDeclaration namespaceDeclaration, object data)
		{
			string name;
			if (currentNamespace.Count == 0) {
				name = namespaceDeclaration.Name;
			} else {
				name = (string)currentNamespace.Peek() + '.' + namespaceDeclaration.Name;
			}
			
			currentNamespace.Push(name);
			object ret = namespaceDeclaration.AcceptChildren(this, data);
			currentNamespace.Pop();
			return ret;
		}
		
		ClassType TranslateClassType(AST.Types type)
		{
			switch (type) {
				case AST.Types.Class:
					return ClassType.Class;
				case AST.Types.Enum:
					return ClassType.Enum;
				case AST.Types.Interface:
					return ClassType.Interface;
				case AST.Types.Struct:
					return ClassType.Struct;
			}
			return ClassType.Class;
		}
		
		DefaultRegion GetRegion(Point start, Point end)
		{
			return new DefaultRegion(start.Y, start.X, end.Y, end.X);
		}
		
		public override object Visit(AST.TypeDeclaration typeDeclaration, object data)
		{
			DefaultRegion region = GetRegion(typeDeclaration.StartLocation, typeDeclaration.EndLocation);
			Class c = new Class(cu, TranslateClassType(typeDeclaration.Type), typeDeclaration.Modifier, region);
			c.Attributes.AddRange(VisitAttributes(typeDeclaration.Attributes));
			
			if (currentClass.Count > 0) {
				Class cur = ((Class)currentClass.Peek());
				cur.InnerClasses.Add(c);
				c.FullyQualifiedName = cur.FullyQualifiedName + '.' + typeDeclaration.Name;
			} else {
				if (currentNamespace.Count == 0) {
					c.FullyQualifiedName = typeDeclaration.Name;
				} else {
					c.FullyQualifiedName = (string)currentNamespace.Peek() + '.' + typeDeclaration.Name;
				}
				cu.Classes.Add(c);
			}
			if (typeDeclaration.BaseTypes != null) {
				foreach (string type in typeDeclaration.BaseTypes) {
					c.BaseTypes.Add(type);
				}
			}
			currentClass.Push(c);
			object ret = typeDeclaration.AcceptChildren(this, data);
			currentClass.Pop();
			c.UpdateModifier();
			return ret;
		}
		
		public override object Visit(AST.DelegateDeclaration delegateDeclaration, object data)
		{
			DefaultRegion region = GetRegion(delegateDeclaration.StartLocation, delegateDeclaration.EndLocation);
			Class c = new Class(cu, ClassType.Delegate, delegateDeclaration.Modifier, region);
			c.Attributes.AddRange(VisitAttributes(delegateDeclaration.Attributes));
			c.BaseTypes.Add("System.Delegate");
			if (currentClass.Count > 0) {
				Class cur = ((Class)currentClass.Peek());
				cur.InnerClasses.Add(c);
				c.FullyQualifiedName = cur.FullyQualifiedName + '.' + delegateDeclaration.Name;
			} else {
				if (currentNamespace.Count == 0) {
					c.FullyQualifiedName = delegateDeclaration.Name;
				} else {
					c.FullyQualifiedName = (string)currentNamespace.Peek() + '.' + delegateDeclaration.Name;
				}
				cu.Classes.Add(c);
			}
			Method invokeMethod = new Method("Invoke", new ReturnType(delegateDeclaration.ReturnType), delegateDeclaration.Modifier, null, null);
			c.Methods.Add(invokeMethod);
			return c;
		}
		
		public override object Visit(AST.MethodDeclaration methodDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(methodDeclaration.StartLocation, methodDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(methodDeclaration.EndLocation, methodDeclaration.Body != null ? methodDeclaration.Body.EndLocation : new Point(-1, -1));
			ReturnType type = new ReturnType(methodDeclaration.TypeReference);
			Class c       = (Class)currentClass.Peek();
			
			Method method = new Method(methodDeclaration.Name, type, methodDeclaration.Modifier, region, bodyRegion);
			method.Attributes.AddRange(VisitAttributes(methodDeclaration.Attributes));
			List<IParameter> parameters = new List<IParameter>();
			if (methodDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in methodDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					Parameter p = new Parameter(par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			method.Parameters = parameters;
			c.Methods.Add(method);
			return null;
		}
		
		public override object Visit(AST.ConstructorDeclaration constructorDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(constructorDeclaration.StartLocation, constructorDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(constructorDeclaration.EndLocation, constructorDeclaration.Body != null ? constructorDeclaration.Body.EndLocation : new Point(-1, -1));
			Class c       = (Class)currentClass.Peek();
			
			Constructor constructor = new Constructor(constructorDeclaration.Modifier, region, bodyRegion);
			constructor.Attributes.AddRange(VisitAttributes(constructorDeclaration.Attributes));
			List<IParameter> parameters = new List<IParameter>();
			if (constructorDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in constructorDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					Parameter p = new Parameter(par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			constructor.Parameters = parameters;
			c.Methods.Add(constructor);
			return null;
		}
		
		public override object Visit(AST.DestructorDeclaration destructorDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(destructorDeclaration.StartLocation, destructorDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(destructorDeclaration.EndLocation, destructorDeclaration.Body != null ? destructorDeclaration.Body.EndLocation : new Point(-1, -1));
			
			Class c       = (Class)currentClass.Peek();
			
			Destructor destructor = new Destructor(c.Name, destructorDeclaration.Modifier, region, bodyRegion);
			destructor.Attributes.AddRange(VisitAttributes(destructorDeclaration.Attributes));
			c.Methods.Add(destructor);
			return null;
		}
		
		
		public override object Visit(AST.FieldDeclaration fieldDeclaration, object data)
		{
			DefaultRegion region = GetRegion(fieldDeclaration.StartLocation, fieldDeclaration.EndLocation);
			Class c = (Class)currentClass.Peek();
			if (currentClass.Count > 0) {
				for (int i = 0; i < fieldDeclaration.Fields.Count; ++i) {
					AST.VariableDeclaration field = (AST.VariableDeclaration)fieldDeclaration.Fields[i];
					
					Field f = new Field(new ReturnType(fieldDeclaration.GetTypeForField(i)), field.Name, fieldDeclaration.Modifier, region);
					f.Attributes.AddRange(VisitAttributes(fieldDeclaration.Attributes));
					if (c.ClassType == ClassType.Enum) {
						f.SetModifiers(ModifierEnum.Const | ModifierEnum.SpecialName);
					}
					
					c.Fields.Add(f);
				}
			}
			return null;
		}
		
		public override object Visit(AST.PropertyDeclaration propertyDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(propertyDeclaration.StartLocation, propertyDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(propertyDeclaration.BodyStart,     propertyDeclaration.BodyEnd);
			
			ReturnType type = new ReturnType(propertyDeclaration.TypeReference);
			Class c = (Class)currentClass.Peek();
			
			Property property = new Property(propertyDeclaration.Name, type, propertyDeclaration.Modifier, region, bodyRegion);
			property.Attributes.AddRange(VisitAttributes(propertyDeclaration.Attributes));
			c.Properties.Add(property);
			return null;
		}
		
		public override object Visit(AST.EventDeclaration eventDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(eventDeclaration.StartLocation, eventDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(eventDeclaration.BodyStart,     eventDeclaration.BodyEnd);
			ReturnType type = new ReturnType(eventDeclaration.TypeReference);
			Class c = (Class)currentClass.Peek();
			Event e = null;
			
			if (eventDeclaration.VariableDeclarators != null) {
				foreach (ICSharpCode.NRefactory.Parser.AST.VariableDeclaration varDecl in eventDeclaration.VariableDeclarators) {
					e = new Event(varDecl.Name, type, eventDeclaration.Modifier, region, bodyRegion);
					e.Attributes.AddRange(VisitAttributes(eventDeclaration.Attributes));
					c.Events.Add(e);
				}
			} else {
				e = new Event(eventDeclaration.Name, type, eventDeclaration.Modifier, region, bodyRegion);
				e.Attributes.AddRange(VisitAttributes(eventDeclaration.Attributes));
				c.Events.Add(e);
			}
			return null;
		}
		
		public override object Visit(AST.IndexerDeclaration indexerDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(indexerDeclaration.StartLocation, indexerDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(indexerDeclaration.BodyStart,     indexerDeclaration.BodyEnd);
			List<IParameter> parameters = new List<IParameter>();
			Indexer i = new Indexer(new ReturnType(indexerDeclaration.TypeReference), parameters, indexerDeclaration.Modifier, region, bodyRegion);
			i.Attributes.AddRange(VisitAttributes(indexerDeclaration.Attributes));
			if (indexerDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in indexerDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					Parameter p = new Parameter(par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			Class c = (Class)currentClass.Peek();
			c.Indexer.Add(i);
			return null;
		}
	}
}
