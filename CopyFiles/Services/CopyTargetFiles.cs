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

namespace CopyFiles.Services;

public class CopyTargetFiles : IDisposable
{
	public IProgressBarService ProgressBarService { get; }
	public CancellationToken CancellationToken { get; }
	public List<TargetFileInformation> TargetFileInfos { get; }

	public CopyTargetFiles( IProgressBarService progressBarService, CancellationToken cancellationToken, List<TargetFileInformation> targetFileInfos )
	{
		ProgressBarService = progressBarService;
		CancellationToken = cancellationToken;
		TargetFileInfos = targetFileInfos;
		ProgressBarService.IsIndeterminate = true;
		ProgressBarService.IsProgressBarVisible = true;
	}
	public void Dispose()
	{
		ProgressBarService.IsProgressBarVisible = false;
	}
	public async Task ExecuteAsync()
	{
		var blockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = CancellationToken,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = -1,	// 受け入れできるだけ受け入れる
		};
		var copyActionBlock = new ActionBlock<TargetFileInformation>( CopyAction, blockOptions );
		ProgressBarService.ProgressMin = 0;
		ProgressBarService.ProgressMax = TargetFileInfos.Count;
		interlockedProgressValue = 0;
		ProgressBarService.ProgressValue = interlockedProgressValue;
		foreach( var targetFile in TargetFileInfos )
		{
			copyActionBlock.Post( targetFile );
		}
		ProgressBarService.IsIndeterminate = false;
		copyActionBlock.Complete();
		await copyActionBlock.Completion;
	}

	private void CopyAction( TargetFileInformation information )
	{
		// 非同期なカウントアップが安全にできるようにしておく
		ProgressBarService.ProgressValue = Interlocked.Increment( ref interlockedProgressValue );
		// 無視する場合はスキップ
		if( !information.Ignore )
		{
			// まだコピーされていない場合は、転送先のフォルダがないかもしれないので作成する
			if( information.Status == TargetStatus.NotExist )
			{
				var dstDir = Path.GetDirectoryName( information.Destination );
				Debug.Assert( dstDir != null ); //	フルパスでセットされているのでnullになることはない
				Directory.CreateDirectory( dstDir );
			}
			// コピーする必要がある場合のみコピーすればよい(同じファイルはコピー不要)
			if( information.Status == TargetStatus.NotExist || information.Status == TargetStatus.Different )
			{
				File.Copy( information.Source, information.Destination, true );
			}
		}
	}

	private int interlockedProgressValue;
}
