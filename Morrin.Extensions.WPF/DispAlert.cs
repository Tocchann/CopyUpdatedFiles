using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Morrin.Extensions.Abstractions;
using System;
using System.Windows;

namespace Morrin.Extensions.WPF;

public class DispAlert : IDispAlert
{
	public static IServiceCollection ConfigureServices( IServiceCollection services )
	{
		services.AddSingleton<IDispAlert, DispAlert>();
		return services;
	}

	/// <summary>
	/// DisplayAlert のキャプションテキスト
	/// </summary>
	public string? Title { get; set; }
	public DispAlert( ILogger<DispAlert>? logger = default )
	{
		m_logger = logger;
	}
	public IDispAlert.Result Show( string message,
		IDispAlert.Buttons button = IDispAlert.Buttons.OK,
		IDispAlert.Icon icon = IDispAlert.Icon.Exclamation,
		IDispAlert.Result defaultResult = IDispAlert.Result.None,
		IDispAlert.Options options = IDispAlert.Options.None )
	{
		// タイトルが設定されていない場合はメインウィンドウのキャプションを利用する。
		string title = Title ?? string.Empty;
		if( string.IsNullOrEmpty( title ) )
		{
			// メインウィンドウの実体があってかつ表示状態(アイコンでもよい)の場合のみタイトルを取り込む
			if( Application.Current.MainWindow != null &&
				Application.Current.MainWindow.Visibility == Visibility.Visible )
			{
				title = Application.Current.MainWindow.Title;
			}
		}
		// メインウィンドウからタイトルを決められない場合やタイトルがついていない場合はモジュール名を利用する
		if( string.IsNullOrEmpty( title ) )
		{
			title = AppDomain.CurrentDomain.FriendlyName;
		}
		return Show( message, title, button, icon, defaultResult, options );
	}
	public IDispAlert.Result Show( string message, string title,
		IDispAlert.Buttons button = IDispAlert.Buttons.OK,
		IDispAlert.Icon icon = IDispAlert.Icon.Exclamation,
		IDispAlert.Result defaultResult = IDispAlert.Result.None,
		IDispAlert.Options options = IDispAlert.Options.None )
	{
		m_logger?.LogInformation( $"WPF.DispAlert.Show( message: {message}, title: {title}, button: {button}, icon: {icon}, defaultResult: {defaultResult}, options: {options})" );

		// ここはタイトルが空でも無視して利用する
		var result = Application.Current.MainWindow != null
			? MessageBox.Show( Application.Current.MainWindow, message, title, (MessageBoxButton)button, (MessageBoxImage)icon, (MessageBoxResult)defaultResult, (MessageBoxOptions)options )
			: MessageBox.Show( message, title, (MessageBoxButton)button, (MessageBoxImage)icon, (MessageBoxResult)defaultResult, (MessageBoxOptions)options );

		return (IDispAlert.Result)result;
	}
	private ILogger<DispAlert>? m_logger;
}
