﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the BSD license (for details please see \src\AddIns\Debugger\Debugger.AddIn\license.txt)

using Debugger.AddIn.Visualizers.Graph.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Debugger.AddIn.Visualizers.Graph
{
	/// <summary>
	/// Store to reuse <see cref="NodeControl" />s so that they don't have to be created for every drawing.
	/// </summary>
	public class NodeControlCache
	{
		public static readonly NodeControlCache Instance = new NodeControlCache();
		
		private Stack<PositionedGraphNodeControl> controls;
		private int controlsReturned = 0;
		
		private NodeControlCache()
		{
			Clear();
		}
		
		public void ReturnForReuse(PositionedGraphNodeControl controlToReuse)
		{
			controls.Push(controlToReuse);
		}
		
		public PositionedGraphNodeControl GetNodeControl()
		{
			controlsReturned++;
			return new PositionedGraphNodeControl();
			// bugs in drawing
			/*var control = controls.Count == 0 ? new PositionedGraphNodeControl() : controls.Pop();
			control.Init();
			control.InvalidateVisual();
			return control;*/
		}
		
		public void Clear()
		{
			controls = new Stack<PositionedGraphNodeControl>();
			controlsReturned = 0;
		}
	}
}
