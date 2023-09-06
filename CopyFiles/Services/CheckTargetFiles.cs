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
		hashAlgorithm = SHA256.Create();
		TargetFileInfos = new();
		ProgressBarService.IsIndeterminate = true;
		ProgressBarService.IsProgressBarVisible = true;
	}
	public void Dispose()
	{
		hashAlgorithm.Dispose();
		ProgressBarService.IsProgressBarVisible = false;
	}

	public async Task ExecuteAsync( IEnumerable<TargetInformation> targetFolderInformations )
	{
		var blockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = CancellationToken,
			EnsureOrdered = false,
		};
		var listupTargetFilesBlock = new TransformManyBlock<TargetInformation, TargetFileInformation>( ListupTargetFiles, blockOptions );
		// 一度ローカルにリストを作ってそこに保持する(数を数える都合)
		var joinTargetFileInfoBlock = new ActionBlock<TargetFileInformation>( JoinTargetFileInfo, blockOptions );

		// 実際のファイルを結合して状態を確認する
		var checkTargetFileStatusBlock = new TransformBlock<TargetFileInformation, TargetFileInformation>( CheckTargetFileStatus, blockOptions );
		var pushTargetFileInfoBlock = new ActionBlock<TargetFileInformation>( PushTargetFileInfo, blockOptions );

		var linkOptions = new DataflowLinkOptions
		{
			PropagateCompletion = true,
		};
		listupTargetFilesBlock.LinkTo( joinTargetFileInfoBlock, linkOptions );
		checkTargetFileStatusBlock.LinkTo( pushTargetFileInfoBlock, linkOptions );

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

		foreach( var info in TargetFileInfos )
		{
			checkTargetFileStatusBlock.Post( info );
		}
		checkTargetFileStatusBlock.Complete();
		await pushTargetFileInfoBlock.Completion;
	}

	private IEnumerable<TargetFileInformation> ListupTargetFiles( TargetInformation information )
	{
		// フォルダを見て、そこの一覧を返す
		var fileInfos = new List<TargetFileInformation>();
		if( Directory.Exists( information.Source ) )
		{
			int skipLen = information.Source.Length;
			// 末尾がディレクトリの区切り記号ではない場合は、さらに１文字スキップ
			if( Path.DirectorySeparatorChar != information.Source[skipLen-1] )
			{
				skipLen++;
			}
			var srcFiles = Directory.EnumerateFiles( information.Source );
			foreach( var srcFile in srcFiles )
			{
				var relFilePath = srcFile.Substring( skipLen );
				fileInfos.Add( new TargetFileInformation
				{
					Source = srcFile,
					Destination = Path.Combine( information.Destination, relFilePath ),
					Status = TargetStatus.Unknown,
					Ignore = false,	//	参照用ファイルパスリストをどこかから取り込んできてそれで無視するフラグを自動設定するのが一番いいんだよね…
				} );
			}
		}
		return fileInfos;
	}
	// 並列に大量のデータを作った後に一つずつ詰め込んでもらう(順番は考慮しない)
	private void JoinTargetFileInfo( TargetFileInformation information )
	{
		lock( TargetFileInfos )
		{
			TargetFileInfos.Add( information );
		}
	}
	private TargetFileInformation CheckTargetFileStatus( TargetFileInformation information )
	{
		// チェックは毎回確認する
		if( information.Ignore == false )
		{
			// コピー先がある場合は、実際に比較する
			if( File.Exists( information.Destination ) )
			{
				// 両方存在する場合のみハッシュで比較する。
				var srcHash = GetFileHash( information.Source );
				var dstHash = GetFileHash( information.Destination );
				if( srcHash != dstHash )
				{
					information.Status = TargetStatus.Different;
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
			else
			{
				information.Status = TargetStatus.NotExist;
			}
		}
		return information;
	}

	private string GetFileHash( string filePath )
	{
		// 計算処理はオンメモリで行う(いろいろ面倒なのでね)
		var fileImage = File.ReadAllBytes( filePath );
		int offset = PeFileService.CalcHashArea( fileImage, out var count );
		var hashBytes = hashAlgorithm.ComputeHash( fileImage, offset, count );
		//	ハッシュは、16進数値文字列化して一意キーとする(.NET5から追加されていたので変更)
		var result = Convert.ToHexString( hashBytes ).ToLower();
		return result;
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
	private HashAlgorithm hashAlgorithm;
	private int interlockedProgressValue;
}
