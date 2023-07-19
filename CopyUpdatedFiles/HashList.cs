using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace CopyUpdatedFiles
{
	public class HashList
	{
		public string? TargetFolder { get; set; }
		public Dictionary<string,string>? FileHashDatas { get; set; }

		public HashAlgorithm CreateHashAlgorithm()
		{
			return SHA256.Create();
		}
		public async Task<bool> LoadListAsync()
		{
			// 空データを用意するだけ(リセットは別作業になる)
			if( string.IsNullOrEmpty( TargetFolder ) )
			{
				FileHashDatas = new();
				return false;
			}
			var jsonPath = Path.Combine( TargetFolder, "HashList.json" );
			if( !string.IsNullOrEmpty( jsonPath ) && File.Exists( jsonPath ) )
			{
				using( var stream = File.OpenRead( jsonPath ) )
				{
					FileHashDatas = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>( stream, default(JsonSerializerOptions) );
				}
			}
			return true;
		}
		public async Task SaveListAsync()
		{
			if( string.IsNullOrEmpty( TargetFolder ) )
			{
				throw new NullReferenceException( $"{nameof(TargetFolder)} is null or empty" );
			}
			var jsonPath = Path.Combine( TargetFolder, "HashList.json" );
			if( FileHashDatas == null || FileHashDatas.Count == 0 )
			{
				if( File.Exists( jsonPath ) )
				{
					File.Delete( jsonPath );
				}
			}
			var options = new JsonSerializerOptions
			{
				Encoder = JavaScriptEncoder.Create( UnicodeRanges.All ),
#if DEBUG
				WriteIndented = true,
#else
				WriteIndented = false,
#endif
			};
			using( var stream = File.Create( jsonPath ))
			{
				await JsonSerializer.SerializeAsync( stream, FileHashDatas, options );
			}
		}
		public async Task ReadFromFilesAsync()
		{
			if( string.IsNullOrEmpty( TargetFolder ) )
			{
				throw new NullReferenceException( $"{nameof( TargetFolder )} is null or empty" );
			}
			FileHashDatas = new();
			using( var hashAlgorithm = CreateHashAlgorithm() )
			{
				int cutLength = TargetFolder.Length + (TargetFolder.Last() == Path.DirectorySeparatorChar ? 0 : 1);
				// ルートは対象外
				var subDirs = Directory.EnumerateDirectories( TargetFolder );
				foreach( var subDir in subDirs )
				{
					await ReadFromFilesAsync( hashAlgorithm, subDir, cutLength );
				}
			}
			await SaveListAsync();
		}
		private async Task ReadFromFilesAsync( HashAlgorithm hashAlgorithm, string searchDir, int cutLength )
		{
			Debug.Assert( FileHashDatas != null );  //	呼び出し元でnullチェックは済ませている(ここまで伝搬されないのがねぇ…)
			var subDirs = Directory.EnumerateDirectories( searchDir );
			foreach( var subDir in subDirs )
			{
				await ReadFromFilesAsync( hashAlgorithm, subDir, cutLength );
			}
			Trace.WriteLine( $"Reading...{searchDir}" );
			var files = Directory.EnumerateFiles( searchDir );
			foreach( var file in files )
			{
				var relPath = file.Substring( cutLength );
				// ファイルハッシュ+ファイルサイズで、完全ユニーク化を目指す
				var hashValue = await hashAlgorithm.GenerateHashFileNameAsync( file );
				FileHashDatas.Add( relPath, hashValue );
			}
		}
	}
}
