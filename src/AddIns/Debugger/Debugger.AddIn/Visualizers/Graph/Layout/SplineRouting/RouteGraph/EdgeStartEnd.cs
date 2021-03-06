﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the BSD license (for details please see \src\AddIns\Debugger\Debugger.AddIn\license.txt)

using System.Collections.Generic;
using System.Linq;
using System;

namespace Debugger.AddIn.Visualizers.Graph.SplineRouting
{
	/// <summary>
	/// Description of EdgeStartEnd.
	/// </summary>
	public class EdgeStartEnd
	{
		public IRect From { get; set; }
		public IRect To { get; set; }
		
		public override bool Equals(object obj)
		{
			var other = obj as EdgeStartEnd;
			if (other == null)
				return false;
			return this.From == other.From && this.To == other.To;
		}
		
		public override int GetHashCode()
		{
			return this.From.GetHashCode() ^ this.To.GetHashCode();
		}
	}
}
