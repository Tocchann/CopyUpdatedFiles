﻿using CopyFiles.Contracts.Services;
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
		var readTargetFileFromIsmBlock = new TransformManyBlock<string,string>( ReadTargetFileFromIsm, blockOptions );
		var joinFocusFilesBlock = new ActionBlock<string>( JoinFocusFiles, blockOptions );
		readTargetFileFromIsmBlock.LinkTo( joinFocusFilesBlock, linkOptions );
	

		// 検索対象フォルダを列挙してファイル一覧をリストアップする
		var listupTargetFilesBlock = new TransformManyBlock<TargetInformation, TargetFileInformation>( ListupTargetFiles, blockOptions );
		var joinTargetFileInfoBlock = new ActionBlock<TargetFileInformation>( JoinTargetFileInfo, blockOptions );
		listupTargetFilesBlock.LinkTo( joinTargetFileInfoBlock, linkOptions );


		// 実際のファイルを結合して状態を確認する
		var checkTargetFileStatusBlock = new TransformBlock<TargetFileInformation, TargetFileInformation>( CheckTargetFileStatus, blockOptions );
		var pushTargetFileInfoBlock = new ActionBlock<TargetFileInformation>( PushTargetFileInfo, blockOptions );

		checkTargetFileStatusBlock.LinkTo( pushTargetFileInfoBlock, linkOptions );

		// ISMを読み取って対象ファイル一覧を構築する
		foreach( var ismFile in targetIsmFiles )
		{
			readTargetFileFromIsmBlock.Post( ismFile );
		}
		readTargetFileFromIsmBlock.Complete();
		await joinFocusFilesBlock.Completion;

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

	private void JoinFocusFiles( string filePath )
	{
		lock( m_focusFileListPath )
		{
			m_focusFileListPath.Add( filePath );
		}
	}

	private IEnumerable<string> ReadTargetFileFromIsm( string ismFile )
	{
		// XMLファイルじゃない場合はそのままファイルパスを返すだけでよい
		if( IsXmlFile( ismFile ) )
		{
			var result = IsmFile.ReadSourceFile( ismFile );
			return result;
		}
		else
		{
			var result = new string[] { ismFile };
			return result;
		}
	}

	private bool IsXmlFile( string ismFile )
	{
		try
		{
			var doc = new XmlDocument();
			doc.Load( ismFile );
			return true;
		}
		catch
		{
		}
		return false;
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
		// デフォルトはコピー対象ファイルすべて
		information.IsCheckTarget = information.NeedCopy;
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
