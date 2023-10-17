using CopyFiles.Contracts.Services;
using CopyFiles.Contracts.Views;
using CopyFiles.Models;
using CopyFiles.Services;
using CopyFiles.ViewModels;
using CopyFiles.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Morrin.Extensions.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace CopyFiles
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
	{
		// App.Current で自分を返すようにしておく
		public static new App Current => (App)Application.Current;

		public T? GetService<T>() where T : class => m_host?.Services.GetService( typeof(T) ) as T;

		public void LoadCollection( ObservableCollection<TargetInformation> collection, string propertyName )
		{
			collection.Clear();
			if( Properties.Contains( propertyName ) )
			{
				// 読み取った時は、JsonElementになっているので変換してやる必要がある(本当は型を見て処理するほうがいいけどここでは省略)
				var jsonElement = (JsonElement?)Properties[propertyName];
				if( jsonElement != null )
				{
					var folderInfos = JsonSerializer.Deserialize<TargetInformation[]>( jsonElement.Value );
					if( folderInfos != null )
					{
						foreach( var info in folderInfos )
						{
							collection.Add( info );
						}
					}
				}
			}
		}
		public bool GetBoolean( string propertyName, bool defaultValue = default(bool) )
		{
			var obj = Properties[propertyName];
			if( obj is JsonElement jsonElement )
			{
				return jsonElement.GetBoolean();
			}
			else if( obj is bool val )
			{
				return val;
			}
			return defaultValue;
		}
		public void LoadTargetIsmFiles( ObservableCollection<string> collection )
		{
			collection.Clear();
			if( App.Current.Properties.Contains( "TargetIsmFiles" ) )
			{
				var jsonElement = (JsonElement?)App.Current.Properties["TargetIsmFiles"];
				if( jsonElement != null )
				{
					var ismFiles = JsonSerializer.Deserialize<string[]>( jsonElement.Value );
					if( ismFiles != null )
					{
						foreach( var file in ismFiles )
						{
							collection.Add( file );
						}
					}
				}
			}
		}


		private async void OnStartupAsync( object sender, StartupEventArgs e )
		{
			m_host = Host.CreateDefaultBuilder(/*e.Args*/)
				.ConfigureAppConfiguration( c => c.SetBasePath( AppDomain.CurrentDomain.BaseDirectory ) )
				.ConfigureServices( OnConfigureServices )
				.Build();

			ConfigureWpfLifeTime( m_host.Services );

			await m_host.StartAsync();
		}

		private void ConfigureWpfLifeTime( IServiceProvider services )
		{
			var logger = services.GetService<ILogger<App>>();
			var lifeTime = services.GetService<IHostApplicationLifetime>();
			if( lifeTime != null )
			{
				// VMから終了を行えるようにしておく
				lifeTime.ApplicationStopping.Register( () => App.Current.MainWindow?.Close() );
			}
		}
		private void OnConfigureServices( HostBuilderContext context, IServiceCollection collection )
		{
			// メッセージボックス
			Morrin.Extensions.WPF.DispAlert.ConfigureServices( collection );

			// フォルダ選択ダイアログ
			Morrin.Extensions.WPF.SelectFolderDialog.ConfigureServices( collection );

			collection.AddHostedService<ApplicationHostService>();

			collection.AddTransient<SelectActionViewModel>();
			collection.AddTransient<ISelectActionView, SelectActionView>();

			collection.AddTransient<CopyFileViewModel>();
			collection.AddTransient<ICopyFileView, CopyFileView>();

			collection.AddTransient<NonSignedFileCopyViewModel>();
			collection.AddTransient<INonSignedFileCopyView, NonSignedFileCopyView>();

			collection.AddTransient<AppendFolderViewModel>();
			collection.AddTransient<IAppendFolderDialog, AppendFolderDialog>();

			collection.AddTransient<IFileService, FileService>();
			collection.AddTransient<IPersistAndRestoreService, PersistAndRestoreService>();

		}
		private async void OnExitAsync( object sender, ExitEventArgs e )
		{
			// Application.Exit イベントが来てる時点でアプリは終わっている(CWinApp::ExitInstanceに当たる処理)
			if( m_host != null )
			{
				await m_host.StopAsync();
			}
		}
		private void OnDispatcherUnhandledException( object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e )
		{
			var alert = GetService<IDispAlert>();
			alert?.Show( e.Exception.ToString() );
			var lifeTime = GetService<IHostApplicationLifetime>();
			Trace.WriteLine( $"lifeTime.ApplicationStarted.IsCancellationRequested={lifeTime?.ApplicationStarted.IsCancellationRequested}" );
			//e.Handled = App.Current.MainWindow != null && App.Current.MainWindow.Visibility == Visibility.Visible;
			e.Handled = lifeTime?.ApplicationStarted.IsCancellationRequested ?? false;
		}
		private IHost? m_host;
	}
}
