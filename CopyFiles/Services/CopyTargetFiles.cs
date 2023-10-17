using CopyFiles.Contracts.Services;
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
using static System.Formats.Asn1.AsnWriter;

namespace CopyFiles.Services;

public class CopyTargetFiles : IDisposable
{
	public IProgressBarService ProgressBarService { get; }
	public CancellationToken CancellationToken { get; }

	public CopyTargetFiles( IProgressBarService progressBarService, CancellationToken cancellationToken )
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
	public async Task ExecuteAsync( List<TargetFileInformation> targetFileInfos )
	{
		var blockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = CancellationToken,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = Environment.ProcessorCount,
		};
		var copyActionBlock = new ActionBlock<TargetFileInformation>( CopyAction, blockOptions );
		ProgressBarService.ProgressMin = 0;
		ProgressBarService.ProgressMax = targetFileInfos.Count;
		ProgressBarService.ProgressValue = interlockedProgressValue;
		interlockedProgressValue = 0;
		ProgressBarService.IsIndeterminate = false;
		foreach( var targetFile in targetFileInfos )
		{
			copyActionBlock.Post( targetFile );
		}
		copyActionBlock.Complete();
		await copyActionBlock.Completion;
	}

	private void CopyAction( TargetFileInformation information )
	{
		// 非同期なカウントアップが安全にできるようにしておく
		ProgressBarService.ProgressValue = Interlocked.Increment( ref interlockedProgressValue );
		// デバッグするときに便利なので全部分けておく
		if( information.Ignore )
		{
			return;
		}
		// まだコピーされていない場合は、転送先のフォルダがないかもしれないので作成する
		if( information.Status == TargetStatus.NotExist )
		{
			var dstDir = Path.GetDirectoryName( information.Destination );
			Debug.Assert( dstDir != null ); //	フルパスでセットされているのでnullになることはない
			Directory.CreateDirectory( dstDir );
		}
		if( information.NeedCopy )
		{
			File.Copy( information.Source, information.Destination, true );
		}
	}

	volatile private int interlockedProgressValue;
}
