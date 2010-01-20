﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Traits;
using System.Reflection;
using IjwFramework.Types;
using System.IO;

namespace OpenRa.GameRules
{
	public class ActorInfo
	{
		public readonly string Name;
		public readonly string Category;
		public readonly TypeDictionary Traits = new TypeDictionary();

		public ActorInfo( string name, MiniYaml node, Dictionary<string, MiniYaml> allUnits )
		{
			var mergedNode = MergeWithParent( node, allUnits ).Nodes;

			Name = name;
			MiniYaml categoryNode;
			if( mergedNode.TryGetValue( "Category", out categoryNode ) )
				Category = categoryNode.Value;

			foreach( var t in mergedNode )
				if( t.Key != "Inherits" && t.Key != "Category" )
					Traits.Add( LoadTraitInfo( t.Key, t.Value ) );
		}

		static MiniYaml GetParent( MiniYaml node, Dictionary<string, MiniYaml> allUnits )
		{
			MiniYaml inherits;
			node.Nodes.TryGetValue( "Inherits", out inherits );
			if( inherits == null || string.IsNullOrEmpty( inherits.Value ) )
				return null;

			MiniYaml parent;
			allUnits.TryGetValue( inherits.Value, out parent );
			if( parent == null )
				return null;

			return parent;
		}

		static MiniYaml MergeWithParent( MiniYaml node, Dictionary<string, MiniYaml> allUnits )
		{
			var parent = GetParent( node, allUnits );
			if( parent != null )
				return MiniYaml.Merge( node, MergeWithParent( parent, allUnits ) );
			return node;
		}

		static Pair<Assembly, string>[] ModAssemblies;
		public static void LoadModAssemblies(Manifest m)
		{
			var asms = new List<Pair<Assembly, string>>();

			// all the core stuff is in this assembly
			asms.Add(Pair.New(typeof(ITraitInfo).Assembly, typeof(ITraitInfo).Namespace));

			// add the mods
			foreach (var a in m.Assemblies)
				asms.Add(Pair.New(Assembly.LoadFile(Path.GetFullPath(a)), Path.GetFileNameWithoutExtension(a)));
			ModAssemblies = asms.ToArray();
		}

		static ITraitInfo LoadTraitInfo(string traitName, MiniYaml my)
		{
			foreach (var mod in ModAssemblies)
			{
				var fullTypeName = mod.Second + "." + traitName + "Info";
				var info = (ITraitInfo)mod.First.CreateInstance(fullTypeName);
				if (info == null) continue;
				FieldLoader.Load(info, my);
				return info;
			}

			throw new InvalidOperationException("Cannot locate trait: {0}".F(traitName));
		}
	}
}
