using CopyFiles.Contracts.Services;
using CopyFiles.Core;
using CopyFiles.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CopyFiles.Services;

public class CopyUnsignedFiles : IDisposable
{
	public IProgressBarService ProgressBarService { get; }
	public CancellationToken CancellationToken { get; }

	public CopyUnsignedFiles( IProgressBarService progressBarService, CancellationToken cancellationToken )
	{
		ProgressBarService = progressBarService;
		CancellationToken = cancellationToken;
		ProgressBarService.IsIndeterminate = true;
		ProgressBarService.IsProgressBarVisible = true;
	}
	public void Dispose()
	{
		ProgressBarService.IsProgressBarVisible = false;
	}
	public async Task ExecuteAsync( IEnumerable<TargetFileInformation> targetFileInfos, IEnumerable<TargetInformation> copyBaseFolders )
	{
		var blockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = CancellationToken,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = Environment.ProcessorCount,
		};
		var copyActionBlock = new ActionBlock<TargetFileInformation>( info => CopyAction( info, copyBaseFolders ), blockOptions );
		ProgressBarService.ProgressMin = 0;
		ProgressBarService.ProgressMax = targetFileInfos.Count();
		interlockedProgressValue = 0;
		ProgressBarService.ProgressValue = interlockedProgressValue;
		foreach( var targetFile in targetFileInfos )
		{
			copyActionBlock.Post( targetFile );
		}
		ProgressBarService.IsIndeterminate = false;
		copyActionBlock.Complete();
		await copyActionBlock.Completion;
	}
	private void CopyAction( TargetFileInformation information, IEnumerable<TargetInformation> copyBaseFolders )
	{
		// 非同期なカウントアップが安全にできるようにしておく
		ProgressBarService.ProgressValue = Interlocked.Increment( ref interlockedProgressValue );
		// デバッグするときに便利なので全部分けておく
		if( information.Ignore )
		{
			return;
		}
		// 署名されていないコピー先ファイルをコピーする
		var fileImage = File.ReadAllBytes( information.Destination );
		if( PeFileService.IsValidPE( fileImage ) && !PeFileService.IsSetSignatgure( fileImage ) )
		{
			// パスの組み立てが若干煩雑…
			var baseFolderInfo = copyBaseFolders.FirstOrDefault( info => information.Destination.Contains( info.Source ) );
			if( baseFolderInfo != null )
			{
				int skipLen = baseFolderInfo.Source.Length;
				if( Path.DirectorySeparatorChar != baseFolderInfo.Source[skipLen - 1] )
				{
					skipLen++;
				}
				var relFilePath = information.Destination.Substring( skipLen );
				var dstFilePath = Path.Combine( baseFolderInfo.Destination, relFilePath );
				var dstDir = Path.GetDirectoryName( dstFilePath );
				if( dstDir != null )
				{
					Directory.CreateDirectory( dstDir );
					File.Copy( information.Destination, dstFilePath, true );
				}
			}
		}
	}
	volatile private int interlockedProgressValue;
}
