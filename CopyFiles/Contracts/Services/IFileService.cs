using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Morrin.Extensions.Abstractions.IDispAlert;

namespace CopyFiles.Contracts.Services;

public interface IFileService
{
	ValueTask<TResult?> ReadAsync<TResult>( string filePath, CancellationToken token = default );
	Task SaveAsync<TValue>( string filePath, TValue content, CancellationToken token = default );
}
