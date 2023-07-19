using System.Diagnostics;
using System.Security.Cryptography;

namespace CopyUpdatedFiles
{
	internal class Program
	{
		static async Task<int> Main( string[] args )
		{
			Trace.Listeners.Add( new ConsoleTraceListener() );
			if( args.Length < 1 )
			{
				Usage();
				return 1;
			}
			var srcFolder = string.Empty;
			var dstFolder = string.Empty;
			bool checkOnly = false;
			bool resetHashList = false;
			foreach( var arg in args )
			{
				if( arg[0] == '-' )
				{
					if( arg.Length > 1 )
					{
						switch( arg[1] )
						{
						case 'c': case 'C':
							checkOnly = true;
							break;
						case 'h': case 'H':
							resetHashList = true;
							break;
						}
					}
				}
				else
				{
					if( string.IsNullOrEmpty( srcFolder ) )
					{
						srcFolder = arg;
					}
					else if( string.IsNullOrEmpty( dstFolder) )
					{
						dstFolder = arg;
					}
				}
			}
			Trace.WriteLine( $"{nameof(srcFolder)}={srcFolder}" );
			Trace.WriteLine( $"{nameof(dstFolder)}={dstFolder}" );
			Trace.WriteLine( $"{nameof(checkOnly)}={checkOnly}" );
			Trace.WriteLine( $"{nameof(resetHashList)}={resetHashList}" );

			var hashList = new HashList
			{
				TargetFolder = dstFolder,
			};
			if( resetHashList )
			{
				await hashList.ReadFromFilesAsync();
			}
			else
			{
				// リセットしない場合はリストファイルがなくても作成しない
				await hashList.LoadListAsync();
			}
			// ハッシュをチェックしながらファイルをコピーする無ければ、新たにコピーする
			using( var hashAlgorithm = hashList.CreateHashAlgorithm() )
			{
				int cutLength = srcFolder.Length + (srcFolder.Last() == Path.DirectorySeparatorChar ? 0 : 1);
				await CopyFilesAsync( hashAlgorithm, hashList, srcFolder, dstFolder, cutLength, checkOnly );
			}
			if( !checkOnly )
			{
				await hashList.SaveListAsync();
			}
			return 0;
		}
		private static async Task CopyFilesAsync( HashAlgorithm hashAlgorithm, HashList hashList, string srcFolder, string dstBaseFolder, int cutLength, bool checkOnly, CancellationToken token = default )
		{
			Debug.Assert( hashList.FileHashDatas != null );
			var subDirs = Directory.EnumerateDirectories( srcFolder );
			foreach( var subDir in subDirs )
			{
				await CopyFilesAsync( hashAlgorithm, hashList, subDir, dstBaseFolder, cutLength, checkOnly, token );
			}
			Trace.WriteLine( $"Check...{srcFolder}" );
			var files = Directory.EnumerateFiles( srcFolder );
			foreach( var filePath in files )
			{
				// ファイルハッシュ+ファイルサイズで、完全ユニーク化を目指す
				var newHashData = await hashAlgorithm.GenerateHashFileNameAsync( filePath );
				var searchPath = filePath.Substring( cutLength );
				// ハッシュリストにない(==新規)または、以前のハッシュ値と異なる(==更新)
				bool isCopy = false;
				if( hashList.FileHashDatas.TryGetValue( searchPath, out var prevHashData ) )
				{
					if( newHashData != prevHashData )
					{
						isCopy = true;
					}
				}
				else
				{
					isCopy = true;
				}
				if( isCopy )
				{
					Trace.WriteLine( $"Copy...{filePath}" );
					if( !checkOnly )
					{
						// 上書き更新でファイルをコピーして、新しいハッシュ値に置き換える
						var dstPath = Path.Combine( dstBaseFolder, searchPath );
						var dstFolder = Path.GetDirectoryName( dstPath );
						Directory.CreateDirectory( dstFolder??"" );
						File.Copy( filePath, dstPath, true );
						hashList.FileHashDatas[searchPath] = newHashData;
					}
				}
			}
		}

		private static void Usage()
		{
			Trace.WriteLine( $"{AppDomain.CurrentDomain.FriendlyName} [コピー元フォルダ] [コピー先フォルダ] -c -h" );
			Trace.WriteLine( "コピー時にハッシュをとって比較することで、署名などがあっても以前の状態を認識できるようにする特殊なコピーコマンド");
			Trace.WriteLine( "-c コピーチェックフラグ(コピーせず、対象の列挙を行う)" );
			Trace.WriteLine( "-h ハッシュをリセットする" );
		}
	}
}