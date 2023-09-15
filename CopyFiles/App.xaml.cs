using CopyFiles.Contracts.Services;
using CopyFiles.Contracts.Views;
using CopyFiles.Services;
using CopyFiles.ViewModels;
using CopyFiles.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Morrin.Extensions.Abstractions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
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
			var lifeTime = services.GetService<IHostApplicationLifetime>();
			if( lifeTime != null )
			{
				// アプリケーションライフタイムの変遷に合わせてメインウィンドウの寿命を制御する
				//lifeTime.ApplicationStarted.Register( () => GetService<IView>()?.ShowWindow() );
				lifeTime.ApplicationStopping.Register( () => App.Current.MainWindow?.Close() );
			}
		}
		private void OnConfigureServices( HostBuilderContext context, IServiceCollection collection )
		{
			Morrin.Extensions.WPF.DispAlert.ConfigureServices( collection );
			Morrin.Extensions.WPF.SelectFolderDialog.ConfigureServices( collection );

			collection.AddSingleton<CopyFileViewModel>();
			collection.AddSingleton<IView, CopyFileView>();
			collection.AddTransient<AppendFolderViewModel>();
			collection.AddTransient<IAppendFolderDialog, AppendFolderDialog>();
			collection.AddTransient<IFileService, FileService>();
			collection.AddTransient<IPersistAndRestoreService, PersistAndRestoreService>();
			collection.AddHostedService<ApplicationHostService>();
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
