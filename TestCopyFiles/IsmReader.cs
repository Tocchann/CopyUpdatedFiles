using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TestCopyFiles
{
	internal class IsmReader
	{
		string m_ismPath;
		XmlDocument m_ism;
		[SetUp]
		public void SetUp()
		{
			m_ismPath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "IKIPInstaller32.ism" );
			m_ism = new XmlDocument();
			m_ism.Load( m_ismPath );
		}
		[Test]
		public void DumpIsPathValue()
		{
			// パス変数を列挙
			var isPathValues = m_ism.SelectNodes( "//table[@name='ISPathVariable']/row" );
			Assert.IsNotNull( isPathValues );
			// row の下は、html互換のテーブルデータになっている
			//<col key="yes" def="s72">ISPathVariable</col> 名前
			//<col def="S255">Value</col> 実際のパス(空の場合はIS固有値)
			//<col def="S255">TestValue</col>
			//<col def="i4">Type</col>
			// 個々のrowデータは以下の形
			//<td>ISPathVariable</td><td>Value</td><td>TestValue</td><td>Type</td>
			foreach( XmlElement row in isPathValues )
			{
				Assert.That( row.ChildNodes.Count, Is.EqualTo( 4 ) );
				Trace.WriteLine( $"{row.ChildNodes[0]?.InnerText}=\"{row.ChildNodes[1]?.InnerText}\"" );
			}
		}
		[Test]
		public void DumpTargetTables()
		{
			// col に ISBuildSourcePathがあるテーブルを検索する(XPathで簡単にできないかな？)
			var targetTables = m_ism.SelectNodes( "//col[text()='ISBuildSourcePath']" );
			Assert.IsNotNull( targetTables );
			// 対象となるテーブルを列挙してそのテーブルごとにファイルをダンプしていく(形になる)
			foreach( XmlElement col in targetTables )
			{
				Trace.WriteLine( $"TargetTable={col.ParentNode?.Attributes?["name"]?.Value}" );
			}
		}
		[Test]
		public void DumpTableKeyColums()
		{
				var targetTables = m_ism.SelectNodes( "//col[text()='ISBuildSourcePath']" );
			Assert.IsNotNull( targetTables );
			var ismFile = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "IKIPInstaller32.ism" );
			var pathVariable = ReadPathVariable();
			// 対象となるテーブルを列挙してそのテーブルごとにファイルをダンプしていく(形になる)
			foreach( XmlElement col in targetTables )
			{
				Assert.IsNotNull( col.ParentNode );
				Assert.IsNotNull( col.ParentNode.Attributes );
				Assert.IsNotNull( col.ParentNode.Attributes["name"] );
				var tableName = col.ParentNode.Attributes["name"].Value;
				Trace.WriteLine( $"TargetTable={tableName}" );
				var cols = m_ism.SelectNodes( $"//table[@name='{tableName}']/col" );
				Assert.IsNotNull( cols );
				int keyIndex = -1;
				int sourcePathIndex = -1;
				for( int index = 0 ; index < cols?.Count ; index++ )
				{
					if( cols[index]?.Attributes?["key"] != null )
					{
						keyIndex = index;
					}
					else if( cols[index]?.InnerText == "ISBuildSourcePath" )
					{
						sourcePathIndex = index;
					}
				}
				// ローデータのインデックスとして列挙する
				var rows = m_ism.SelectNodes( $"//table[@name='{tableName}']/row" );
				if( rows != null )
				{
					foreach( XmlElement row in rows )
					{
						Assert.IsTrue( row.ChildNodes.Count > keyIndex );
						Assert.IsTrue( row.ChildNodes.Count > sourcePathIndex );
						var key = row.ChildNodes[keyIndex]?.InnerText;
						var sourcePath  = row.ChildNodes[sourcePathIndex]?.InnerText;
						if( !string.IsNullOrEmpty( sourcePath ) )
						{
							// 対象パスを取得したので、パス変換テーブルを通して物理パスにする
							foreach( var kv in pathVariable )
							{
								sourcePath = sourcePath.Replace( kv.Key, kv.Value );
							}
							Trace.WriteLine( $"  {key}={sourcePath}" );
						}
					}
				}
			}
		}
		private Dictionary<string, string> ReadPathVariable()
		{
			var pathVariable = new Dictionary<string, string>();
			var isPathVariables = m_ism.SelectNodes( "//table[@name='ISPathVariable']/row" );
			if( isPathVariables != null )
			{
				foreach( XmlElement row in isPathVariables )
				{
					if( row.ChildNodes.Count >= 2 )
					{
						var key = row.ChildNodes[0]?.InnerText;
						var value = row.ChildNodes[1]?.InnerText;
						// ISProjectFolder はismのパスをベースとしたものになる
						if( key == "ISProjectFolder" )
						{
							value = Path.GetDirectoryName( m_ismPath );
						}
						// 空のキーは変換しないので、セットしない(セットするとパスが壊れてしまう)
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
	}
}
