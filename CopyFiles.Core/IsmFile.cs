using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CopyFiles.Core
{
	public static class IsmFile
	{
		public static HashSet<string> ReadSourceFile( string ismPath )
		{
			var result = new HashSet<string>();
			var ism = new XmlDocument();
			ism.Load( ismPath );
			var pathVariable = ReadPathVariable( ismPath, ism );
			var nodes = ism.SelectNodes( "//col[text()='ISBuildSourcePath']" );
			if( nodes == null )
			{
				return result;
			}
			foreach( XmlElement node in nodes )
			{
				var tableName = node.ParentNode?.Attributes?["name"]?.Value;
				int index = GetISBuildSourcePathIndex( ism, tableName );
				if( index != -1 )
				{
					Trace.WriteLine( $"//table[@name='{tableName}']" );
					var rows = ism.SelectNodes( $"//table[@name='{tableName}']/row" );
					if( rows != null )
					{
						foreach( XmlElement row in rows )
						{
							var sourcePath = row.ChildNodes[index]?.InnerText;
							if( !string.IsNullOrEmpty( sourcePath ) )
							{
								// 対象パスを取得したので、パス変換テーブルを通して物理パスにする
								foreach( var kv in pathVariable )
								{
									sourcePath = sourcePath.Replace( kv.Key, kv.Value );
								}
								if( !sourcePath.Contains( '<' ) )
								{
									result.Add( sourcePath );
									Trace.WriteLine( $"Add:{sourcePath}" );
								}
							}
						}
					}
				}
			}
			return result;
		}
		private static Dictionary<string, string> ReadPathVariable( string ismPath, XmlDocument ism )
		{
			var pathVariable = new Dictionary<string, string>();
			var isPathVariables = ism.SelectNodes( "//table[@name='ISPathVariable']/row" );
			if( isPathVariables != null )
			{
				foreach( XmlElement row in isPathVariables )
				{
					if( row.ChildNodes.Count >= 2 )
					{
						var key = row.ChildNodes[0]?.InnerText;
						var value = row.ChildNodes[1]?.InnerText;
						if( key == "ISProjectFolder" )
						{
							value = Path.GetDirectoryName( ismPath );
						}
						if( string.IsNullOrEmpty( key ) == false && string.IsNullOrEmpty( value ) == false )
						{
							// キーはあとで単純変換できるようにするために<>をつけておく
							pathVariable["<" + key + ">"] = value;
						}
					}
				}
			}
			return pathVariable;
		}
		private static int GetISBuildSourcePathIndex( XmlDocument ism, string? tableName )
		{
			if( string.IsNullOrEmpty( tableName ) )
			{
				return -1;
			}
			var cols = ism.SelectNodes( $"//table[@name='{tableName}']/col" );
			if( cols == null )
			{
				return -1;
			}
			for( int index = 0 ; index < cols.Count ; index++ )
			{
				if( cols[index]?.InnerText == "ISBuildSourcePath" )
				{
					return index;
				}
			}
			return -1;
		}
	}
}
