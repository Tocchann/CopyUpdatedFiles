using System.Diagnostics;
using System.Security.Cryptography;

namespace CopyUpdatedFiles
{
	public static class HashAlgorithmExtention
	{
		public static async Task<string> GenerateHashFileNameAsync( this HashAlgorithm hashAlgorithm, string filePath )
		{
			try
			{
				var fileInfo = new FileInfo( filePath );
				using( var stream = File.OpenRead( filePath ) )
				{
					var hashBytes = await hashAlgorithm.ComputeHashAsync( stream );
					//	ハッシュは、16進数値文字列化して一意キーとする(.NET5から追加されていたので変更)
					var lengthBytes = new byte[sizeof(long) / sizeof( byte )];
					var fileLen = fileInfo.Length;
					for( int index = 0 ; index < lengthBytes.Length ; index++ )
					{
						lengthBytes[index] = (byte)(fileLen & 0xFF);
						fileLen >>= 8;
					}
					var result = (Convert.ToHexString( lengthBytes ) + Convert.ToHexString( hashBytes ));
					return result;
				}
			}
			catch
			{
				Trace.WriteLine( "Fail:GenerateHashFileName({filePath})" );
				throw;
			}
		}
	}
}
