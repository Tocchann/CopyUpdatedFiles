using CopyFiles.Contracts.Services;
using CopyFiles.Core;
using CopyFiles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml;

namespace CopyFiles.Services;

public class CheckTargetFiles : IDisposable
{
	public IProgressBarService ProgressBarService { get; }
	public CancellationToken CancellationToken { get; }
	public List<TargetFileInformation> TargetFileInfos { get; }

	public CheckTargetFiles( IProgressBarService progressBarService, CancellationToken token )
	{
		ProgressBarService = progressBarService;
		CancellationToken = token;
		TargetFileInfos = new();
		m_focusFileListPath = new();
		ProgressBarService.IsIndeterminate = true;
		ProgressBarService.IsProgressBarVisible = true;
	}
	public void Dispose()
	{
		ProgressBarService.IsProgressBarVisible = false;
	}

	public async Task ExecuteAsync( IEnumerable<TargetInformation> targetFolderInformations, IEnumerable<string> targetIsmFiles )
	{
		var blockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = CancellationToken,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = Environment.ProcessorCount,
		};
		var linkOptions = new DataflowLinkOptions
		{
			PropagateCompletion = true,
		};

		// 対象ファイルを絞り込みするためのパスリストをセットする
		var readTargetFileFromIsmBlock = new ActionBlock<string>( ReadTargetFileFromIsm, blockOptions );
	

		// 検索対象フォルダを列挙してファイル一覧をリストアップする
		var listupTargetFilesBlock = new TransformManyBlock<TargetInformation, TargetFileInformation>( ListupTargetFiles, blockOptions );
		var joinTargetFileInfoBlock = new ActionBlock<TargetFileInformation>( JoinTargetFileInfo, blockOptions );
		listupTargetFilesBlock.LinkTo( joinTargetFileInfoBlock, linkOptions );


		// 実際のファイルを結合して状態を確認する
		var checkTargetFileStatusBlock = new TransformBlock<TargetFileInformation, TargetFileInformation>( CheckTargetFileStatus, blockOptions );
		var pushTargetFileInfoBlock = new ActionBlock<TargetFileInformation>( PushTargetFileInfo, blockOptions );

		checkTargetFileStatusBlock.LinkTo( pushTargetFileInfoBlock, linkOptions );

		// フォーカスリストの読み込み
		foreach( var ismFile in targetIsmFiles )
		{
			readTargetFileFromIsmBlock.Post( ismFile );
		}
		readTargetFileFromIsmBlock.Complete();
		await readTargetFileFromIsmBlock.Completion;

		// 対象ファイルの列挙
		foreach( var folderInfo in targetFolderInformations )
		{
			listupTargetFilesBlock.Post( folderInfo );
		}
		listupTargetFilesBlock.Complete();
		await joinTargetFileInfoBlock.Completion;

		ProgressBarService.ProgressMin = 0;
		ProgressBarService.ProgressMax = TargetFileInfos.Count;
		interlockedProgressValue = 0;
		ProgressBarService.ProgressValue = interlockedProgressValue;
		ProgressBarService.IsIndeterminate = false;

		// 実際のファイルを比較して処理の必要があるかを確認する
		foreach( var info in TargetFileInfos )
		{
			checkTargetFileStatusBlock.Post( info );
		}
		checkTargetFileStatusBlock.Complete();
		await pushTargetFileInfoBlock.Completion;
	}

	private void ReadTargetFileFromIsm( string ismFile )
	{
		// ismFile を読み取って、対象ファイル一覧をセットする
		// XmlDocument で読み取ってくれないのは
		var ism = new XmlDocument();
		ism.Load( ismFile );
		// ISPathVariable をリストアップしてパス変換テーブルを用意する
		var pathVariable = ReadPathVariable( ismFile, ism );
		// テーブルリストを取り出す
		var nodes = ism.SelectNodes( "//col[text()='ISBuildSourcePath']" );
		if( nodes == null )
		{
			return;
		}
		// 実際のテーブルをサーチしながらファイル一覧を取り込んでいく
		foreach( XmlElement col in nodes )
		{
			// 構造上絶対にある
			var tableName = col.ParentNode?.Attributes?["name"]?.Value; // nullable をチェックしなくてよい
			int index = GetISBuildSourcePathIndex( ism, tableName );
			if( index != -1 )
			{
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
								m_focusFileListPath.Add( sourcePath );
							}
						}
					}
				}
			}
		}

		//var fileList = await File.ReadAllLinesAsync( filePath );
		//foreach( var file in fileList )
		//{
		//	m_focusFileListPath.Add( file );
		//}
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

	private static Dictionary<string, string> ReadPathVariable( string ismFile, XmlDocument ism )
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
						value = Path.GetDirectoryName( ismFile );
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

	private IEnumerable<TargetFileInformation> ListupTargetFiles( TargetInformation information )
	{
		var targetFileInfos = new List<TargetFileInformation>();
		if( Directory.Exists( information.Source ) )
		{
			int skipLen = information.Source.Length;
			// 末尾がディレクトリの区切り記号ではない場合は、さらに１文字スキップ
			if( Path.DirectorySeparatorChar != information.Source[skipLen - 1] )
			{
				skipLen++;
			}
			ListupTargetFiles( targetFileInfos, information, skipLen, information.Source);
		}
		return targetFileInfos;
	}
	private void ListupTargetFiles( List<TargetFileInformation> targetFileInfos, TargetInformation information, int skipLen, string searchFolder )
	{
		// サブフォルダがあればドリルダウンする
		var subDirs = Directory.EnumerateDirectories( searchFolder );
		foreach( var subDir in subDirs )
		{
			ListupTargetFiles( targetFileInfos, information, skipLen, subDir );
		}
		// フォルダを見て、そこの一覧を返す
		var srcFiles = Directory.EnumerateFiles( searchFolder );
		foreach( var srcFile in srcFiles )
		{
			var relFilePath = srcFile.Substring( skipLen );
			targetFileInfos.Add( new TargetFileInformation
			{
				Source = srcFile,
				Destination = Path.Combine( information.Destination, relFilePath ),
				Status = TargetStatus.Unknown,
				Ignore = false,	//	参照用ファイルパスリストをどこかから取り込んできてそれで無視するフラグを自動設定するのが一番いいんだよね…
			} );
		}
	}
	// 並列に大量のデータを作った後に一つずつ詰め込んでもらう(順番は考慮しない)
	private void JoinTargetFileInfo( TargetFileInformation information )
	{
		//	リストが指定されている場合はリストになければ除外する
		if( m_focusFileListPath.Count != 0 )
		{
			information.Ignore = m_focusFileListPath.Contains( information.Destination ) ? false : true ;
		}
		lock( TargetFileInfos )
		{
			TargetFileInfos.Add( information );
		}
	}
	private TargetFileInformation CheckTargetFileStatus( TargetFileInformation information )
	{
		// チェックは毎回確認する
		//if( information.Ignore == false )	// 検査では無視しない
		{
			information.SourceVersion = GetFileVesrion( information.Source );
			// コピー先がある場合は、実際に比較する
			if( File.Exists( information.Destination ) )
			{
				information.DestinationVersion = GetFileVesrion( information.Destination );
				// 両方存在する場合のみハッシュで比較する。
				using( var hashAlgorithm = SHA256.Create() )
				{
					var srcHash = GetFileHash( hashAlgorithm, information.Source );
					var dstHash = GetFileHash( hashAlgorithm, information.Destination );
					if( srcHash != dstHash )
					{
						information.Status =
							information.SourceVersion != null && information.SourceVersion == information.DestinationVersion
								? TargetStatus.DifferentSameVer
								: TargetStatus.Different;
					}
					else
					{
						// 内容は一致するが日付などが異なっている場合のフラグ判定
						var srcInfo = new FileInfo( information.Source );
						var dstInfo = new FileInfo( information.Destination );
						information.Status =
							srcInfo.Length != dstInfo.Length
								? TargetStatus.SameWithoutSize
								: srcInfo.LastWriteTimeUtc != dstInfo.LastWriteTimeUtc
									? TargetStatus.SameWithoutDate
									: TargetStatus.SameFullMatch;
					}
				}
			}
			else
			{
				information.Status = TargetStatus.NotExist;
			}
		}
		return information;
	}

	private string GetFileHash( HashAlgorithm hashAlgorithm, string filePath )
	{
		// 計算処理はオンメモリで行う(いろいろ面倒なのでね)
		var fileImage = File.ReadAllBytes( filePath );
		int offset = PeFileService.CalcHashArea( fileImage, out var count );
		var hashBytes = hashAlgorithm.ComputeHash( fileImage, offset, count );
		//	ハッシュは、16進数値文字列化して一意キーとする(.NET5から追加されていたので変更)
		var result = Convert.ToHexString( hashBytes ).ToLower();
		return result;
	}
	private Version? GetFileVesrion( string filePath )
	{
		var verInfo = FileVersionInfo.GetVersionInfo( filePath );
		if( !string.IsNullOrEmpty( verInfo.FileVersion ) )
		{
			return new Version( verInfo.FileMajorPart, verInfo.FileMinorPart, verInfo.FileBuildPart, verInfo.FilePrivatePart );
		}
		return null;
	}


	private void PushTargetFileInfo( TargetFileInformation information )
	{
		ProgressBarService.ProgressValue = Interlocked.Increment( ref interlockedProgressValue );
		//// ここは、単純設定するだけ(前回の情報は保持しておきたいけどどうする？)
		//var existInfo = ProgressBarService.TargetFileInformationCollection.FirstOrDefault( info => info.Source == information.Source );
		//if( existInfo == null )
		//{
		//	ProgressBarService.TargetFileInformationCollection.Add( information );
		//}
		//else
		//{
		//	existInfo.Ignore = information.Ignore;
		//	existInfo.Status = information.Status;
		//}
	}
	private int interlockedProgressValue;
	private HashSet<string> m_focusFileListPath;
}
