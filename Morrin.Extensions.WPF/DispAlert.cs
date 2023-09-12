using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Morrin.Extensions.Abstractions;
using Morrin.Extensions.WPF.Interops;
using System;
using System.Windows;
using static Morrin.Extensions.WPF.Interops.NativeMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

		// TODO:TaskDialog APIをつかって、PerMonitorに対応した形で表現する(ただし、ボタンパターンによっては表現できないんだよね…どうしよう？) 
		IntPtr ownerWindow = NativeMethods.GetSafeOwnerWindow( Utilities.GetOwnerWindow() );
		var tdcf = button switch
		{
			IDispAlert.Buttons.OK => TaskDialogCommonButtonFlags.Ok,
			IDispAlert.Buttons.OKCancel => TaskDialogCommonButtonFlags.Ok | TaskDialogCommonButtonFlags.Cancel,
			IDispAlert.Buttons.YesNo => TaskDialogCommonButtonFlags.Yes | TaskDialogCommonButtonFlags.No,
			IDispAlert.Buttons.YesNoCancel => TaskDialogCommonButtonFlags.Yes | TaskDialogCommonButtonFlags.No | TaskDialogCommonButtonFlags.Cancel,
			_ => throw new NotImplementedException(),
		};
		var nativeIcon = icon switch
		{
			IDispAlert.Icon.None => IntPtr.Zero,
			IDispAlert.Icon.Error => NativeMethods.MAKEINTRESOURCE( -2 ), // == TD_ERROR_ICON
			IDispAlert.Icon.Question => NativeMethods.MAKEINTRESOURCE( 32514 ), // == IDI_QUESTION
			IDispAlert.Icon.Exclamation => NativeMethods.MAKEINTRESOURCE( -1 ), // == TD_WARNING_ICON
			IDispAlert.Icon.Asterisk => NativeMethods.MAKEINTRESOURCE( -3 ), // TD_INFORMATION_ICON
			_ => throw new NotImplementedException(),
		};
		// アイコンリソースはシステムリソースしか使わないのでインスタンスはいらない
		NativeMethods.TaskDialog( ownerWindow, IntPtr.Zero, title, string.Empty, message, tdcf, nativeIcon, out var result );
		return result;
	}
	private ILogger<DispAlert>? m_logger;
}
