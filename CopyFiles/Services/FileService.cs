using CopyFiles.Contracts.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using static Morrin.Extensions.Abstractions.IDispAlert;

namespace CopyFiles.Services;

public class FileService : IFileService
{
	public async ValueTask<TResult?> ReadAsync<TResult>( string filePath, CancellationToken token = default )
	{
		// 指定されたファイルから読み取る
		if( Path.Exists( filePath ) )
		{
			using( var stream = File.OpenRead( filePath ) )
			{
				JsonSerializerOptions? options = null;
				return await JsonSerializer.DeserializeAsync<TResult>( stream, options, token );
			}
		}
		return default;
	}

	public async Task SaveAsync<TValue>( string filePath, TValue content, CancellationToken token = default )
	{
		Directory.CreateDirectory( Path.GetDirectoryName( filePath )??string.Empty );
		using( var stream = File.Create( filePath ))
		{
			// UNICODE文字をエスケープしない、インデントをつける(手動修正しやすくしておく)
			var options = new JsonSerializerOptions
			{
				Encoder = JavaScriptEncoder.Create( UnicodeRanges.All ),
				WriteIndented = true,
			};
			await JsonSerializer.SerializeAsync<TValue>( stream, content, options, token );
		}
	}
}
