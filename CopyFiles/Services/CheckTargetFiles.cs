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
		ProgressBarService.IsIndeterminate = true;
		ProgressBarService.IsProgressBarVisible = true;
	}
	public void Dispose()
	{
		ProgressBarService.IsProgressBarVisible = false;
	}

	public async Task ExecuteAsync( bool checkCopyTargets, IEnumerable<TargetInformation> targetFolderInformations, IEnumerable<string> targetIsmFiles )
	{
		// ISMを読み取って、対象ファイル一覧を構築する
		var readIsmFileList = new AsyncReadIsmFileList();
		var focusFiles = await readIsmFileList.ReadIsmFilesAsync( targetIsmFiles, CancellationToken );

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
		// 検索対象フォルダを列挙してファイル一覧をリストアップする
		var listupTargetFilesBlock = new TransformManyBlock<TargetInformation, TargetFileInformation>( ListupTargetFiles, blockOptions );
		var joinTargetFileInfoBlock =
			checkCopyTargets ? new ActionBlock<TargetFileInformation>( info => SetDistinationIgnoreFile( info, focusFiles ), blockOptions )
							 : new ActionBlock<TargetFileInformation>( info => SetSourceIgnoreFile( info, focusFiles ), blockOptions );

		listupTargetFilesBlock.LinkTo( joinTargetFileInfoBlock, linkOptions );

		// ファイルの情報詳細を構築する
		var checkTargetFileStatusBlock = new TransformBlock<TargetFileInformation, TargetFileInformation>( CheckTargetFileStatus, blockOptions );
		var checkUnsignedFileStatusBlock = new TransformBlock<TargetFileInformation, TargetFileInformation>( CheckUnsignedFileStatus, blockOptions );
		var pushTargetFileInfoBlock = new ActionBlock<TargetFileInformation>( PushTargetFileInfo, blockOptions );
		if( checkCopyTargets )
		{
			checkTargetFileStatusBlock.LinkTo( pushTargetFileInfoBlock, linkOptions );
		}
		else
		{
			checkTargetFileStatusBlock.LinkTo( checkUnsignedFileStatusBlock, linkOptions );
			checkUnsignedFileStatusBlock.LinkTo( pushTargetFileInfoBlock, linkOptions );
		}
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
		// 実際のファイルの状態を取得して、コピー対象かどうかを比較する
		foreach( var info in TargetFileInfos )
		{
			checkTargetFileStatusBlock.Post( info );
		}
		checkTargetFileStatusBlock.Complete();
		await pushTargetFileInfoBlock.Completion;
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
			var dstFilePath = Path.Combine( information.Destination, relFilePath );
			targetFileInfos.Add( new TargetFileInformation
			{
				Source = srcFile,
				Destination = dstFilePath,
				Status = TargetStatus.Unknown,
				Ignore = false, //	参照用ファイルパスリストをどこかから取り込んできてそれで無視するフラグを自動設定するのが一番いいんだよね…
				SourceOffsetPos = skipLen,
				DestinationOffsetPos = dstFilePath.Length-relFilePath.Length,
			} );
		}
	}
	// コピー先が対象かどうかをチェック
	private void SetDistinationIgnoreFile( TargetFileInformation information, HashSet<string> focusFiles )
	{
		//	リストがあってデータが含まれていない場合は無視
		information.Ignore = focusFiles.Count != 0 && focusFiles.Contains( information.Destination ) == false;
		lock( TargetFileInfos )
		{
			TargetFileInfos.Add( information );
		}
	}
	// コピー元が対象かどうかをチェック
	private void SetSourceIgnoreFile( TargetFileInformation information, HashSet<string> focusFiles )
	{
		//	リストがあってデータが含まれていない場合は無視
		information.Ignore = focusFiles.Count != 0 && focusFiles.Contains( information.Source ) == false;
		lock( TargetFileInfos )
		{
			TargetFileInfos.Add( information );
		}
	}
	
	private TargetFileInformation CheckTargetFileStatus( TargetFileInformation information )
	{
		// チェックは毎回確認する
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
		return information;
	}
	private TargetFileInformation CheckUnsignedFileStatus( TargetFileInformation information )
	{
		// コピー対象だけ列挙すればよい
		if( information.Ignore == false && information.NeedCopy )
		{
			information.Ignore = true;
			// 未署名のものだけコピーするようにすればよい
			var fileImage = File.ReadAllBytes( information.Source );
			if( PeFileService.IsValidPE( fileImage ) )
			{
				// 署名されているかどうかがキーポイントになる
				if( !PeFileService.IsSetSignatgure( fileImage ) )
				{
					information.Status = TargetStatus.NotSigned;
					information.Ignore = false;
				}
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
	}
	private int interlockedProgressValue;
}
