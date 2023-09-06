using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CopyFiles.Contracts.Services;

public interface IPersistAndRestoreService
{
	Task PersistDataAsync( CancellationToken token = default );
	Task RestoreDataAsync( CancellationToken token = default );
}
