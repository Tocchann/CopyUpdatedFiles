using CopyFiles.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml;

namespace CopyFiles.Services;

public class AsyncReadIsmFileList
{
	public async Task<HashSet<string>> ReadIsmFilesAsync( IEnumerable<string> targetIsmFiles, CancellationToken cancelToken )
	{
		var blockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = cancelToken,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = -1,
		};
		var linkOptions = new DataflowLinkOptions
		{
			PropagateCompletion = true,
		};
		// 対象ファイルを絞り込みするためのパスリストをセットする
		var targetFiles = new HashSet<string>();
		var readTargetFileFromIsmBlock = new TransformManyBlock<string, string>( ReadTargetFileFromIsm, blockOptions );
		var joinFocusFilesBlock = new ActionBlock<string>( file => JoinFocusFiles( file, targetFiles ), blockOptions );
		readTargetFileFromIsmBlock.LinkTo( joinFocusFilesBlock, linkOptions );

		// ISMを読み取って対象ファイル一覧を構築する
		foreach( var ismFile in targetIsmFiles )
		{
			readTargetFileFromIsmBlock.Post( ismFile );
		}
		readTargetFileFromIsmBlock.Complete();
		await joinFocusFilesBlock.Completion;

		return targetFiles;
	}
	static private IEnumerable<string> ReadTargetFileFromIsm( string ismFile )
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
	static private bool IsXmlFile( string ismFile )
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
	private void JoinFocusFiles( string filePath, HashSet<string> targetFiles )
	{
		lock( targetFiles )
		{
			targetFiles.Add( filePath );
		}
	}
}
