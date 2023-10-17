using CopyFiles.Contracts.Services;
using CopyFiles.Contracts.Views;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CopyFiles.Services;

internal class ApplicationHostService : IHostedService
{
	public async Task StartAsync( CancellationToken cancellationToken )
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		await m_persistAndRestoreService.RestoreDataAsync( cancellationToken );
		App.Current.GetService<ISelectActionView>()?.ShowWindow();
		await Task.CompletedTask;
	}

	public async Task StopAsync( CancellationToken cancellationToken )
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		await m_persistAndRestoreService.PersistDataAsync( cancellationToken );
	}

	public ApplicationHostService( ILogger<ApplicationHostService> logger, IPersistAndRestoreService persistAndRestoreService )
	{
		m_logger = logger;
		m_persistAndRestoreService = persistAndRestoreService;
	}
	private ILogger<ApplicationHostService> m_logger;
	private IPersistAndRestoreService m_persistAndRestoreService;
}
