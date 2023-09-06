using CopyFiles.Contracts.Services;
using CopyFiles.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CopyFiles.Services;

public class PersistAndRestoreService : IPersistAndRestoreService
{
	public async Task PersistDataAsync( CancellationToken token = default )
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		await m_fileService.SaveAsync( GetPersistFilePath(), App.Current.Properties, token );
	}
	public async Task RestoreDataAsync( CancellationToken token = default )
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		var props = await m_fileService.ReadAsync<IDictionary>( GetPersistFilePath(), token );
		if( props != null )
		{
			foreach( DictionaryEntry entry in props )
			{
				App.Current.Properties.Add( entry.Key, entry.Value );
			}
		}
		await Task.CompletedTask;
	}
	public PersistAndRestoreService( ILogger<PersistAndRestoreService> logger, IFileService fileService, IHostEnvironment env )
	{
		m_logger = logger;
		m_fileService = fileService;
		m_environment = env;
	}
	private string GetPersistFilePath() => Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), m_environment.ApplicationName, "AppProperties.json" );
	private ILogger<PersistAndRestoreService> m_logger;
	private IFileService m_fileService;
	private IHostEnvironment m_environment;
}
