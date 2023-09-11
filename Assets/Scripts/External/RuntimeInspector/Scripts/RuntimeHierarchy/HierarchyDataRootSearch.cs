﻿using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class HierarchyDataRootSearch : HierarchyDataRoot
	{
		public override string Name { get { return reference.Name; } }
		public override int ChildCount { get { return searchResult.Count; } }

		private readonly List<Transform> searchResult = new List<Transform>();

		private readonly HierarchyDataRoot reference;

		private string searchTerm;

		public HierarchyDataRootSearch( RuntimeHierarchy hierarchy, HierarchyDataRoot reference ) : base( hierarchy )
		{
			this.reference = reference;
		}

		public override void RefreshContent()
		{
			if( !Hierarchy.IsInSearchMode )
				return;

			searchResult.Clear();
			searchTerm = Hierarchy.SearchTerm;

			int childCount = reference.ChildCount;
			for( int i = 0; i < childCount; i++ )
			{
				Transform obj = reference.GetChild( i );
				if( !obj )
					continue;

				if( RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Contains( obj.transform ) )
					continue;

				if( obj.name.IndexOf( searchTerm, System.StringComparison.OrdinalIgnoreCase ) >= 0 )
					searchResult.Add( obj );

				SearchTransformRecursively( obj.transform );
			}
		}

		public override bool Refresh()
		{
			m_depth = 0;
			bool result = base.Refresh();

			// Scenes with no matching search results should be hidden in search mode
			if( searchResult.Count == 0 )
			{
				m_height = 0;
				m_depth = -1;
			}

			return result;
		}

		public override HierarchyDataTransform FindTransformInVisibleChildren( Transform target, int targetDepth = -1 )
		{
			if( m_depth < 0 || targetDepth > 1 || !IsExpanded )
				return null;

			for( int i = children.Count - 1; i >= 0; i-- )
			{
				if( ReferenceEquals( children[i].BoundTransform, target ) )
					return children[i];
			}

			return null;
		}

		private void SearchTransformRecursively( Transform obj )
		{
			for( int i = 0; i < obj.childCount; i++ )
			{
				Transform child = obj.GetChild( i );
				if( RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Contains( child ) )
					continue;

				if( child.name.IndexOf( searchTerm, System.StringComparison.OrdinalIgnoreCase ) >= 0 )
					searchResult.Add( child );

				SearchTransformRecursively( child );
			}
		}

		public override Transform GetChild( int index )
		{
			return searchResult[index];
		}

		public override Transform GetNearestRootOf( Transform target )
		{
			return searchResult.Contains( target ) ? target : null;
		}
	}
}