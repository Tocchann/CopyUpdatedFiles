using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Models;
using Microsoft.Extensions.Logging;
using Morrin.Extensions.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace CopyFiles.ViewModels;

public partial class AppendFolderViewModel : ObservableValidator
{
	[ObservableProperty]
	[CustomValidation(typeof( AppendFolderViewModel ), nameof( ValidateSource ))]
	string? source;

	public static ValidationResult? ValidateSource( string newValue, ValidationContext context )
	{
		var vm = (AppendFolderViewModel)context.ObjectInstance;
		if( !vm.IsDetectExistTarget( info => info.Source, newValue ) )
		{
			return new( "同じコピー元がすでに存在します" );
		}

		return ValidationResult.Success;
	}
	[ObservableProperty]
	[CustomValidation( typeof( AppendFolderViewModel ), nameof( ValidateDestination ))]
	string? destination;

	public static ValidationResult? ValidateDestination( string newValue, ValidationContext context )
	{
		var vm = (AppendFolderViewModel)context.ObjectInstance;
		//if( !vm.IsDetectExistTarget( info => info.Destination, newValue ) )
		//{
		//	return new( "同じコピー先がすでに存在します" );
		//}
		return ValidationResult.Success;
	}
	[ObservableProperty]
	string? dialogTitle;

	/// <summary>
	/// ダイアログ側でボタンハンドラを用意するのではなく、VM側に持たせ
	/// </summary>
	[ObservableProperty]
	bool? dialogResult;

	[RelayCommand]
	void SelectFolder(string target)
	{
		m_logger.LogInformation( $"{System.Reflection.MethodBase.GetCurrentMethod()?.Name}( target:{target} )" );
		var dlg = App.Current.GetService<ISelectFolderDialog>();
		if( dlg != null )
		{
			dlg.Title = $"{target} フォルダの選択";
			dlg.SelectedPath = target == "コピー元" ? Source : Destination ;
			if( dlg.ShowDialog() != false )
			{
				if( target == "コピー元" )
				{
					Source = dlg.SelectedPath;
					//if( IsDetectExistTarget( info => info.Source, dlg.SelectedPath ) )
					//{
					//	Source = dlg.SelectedPath;
					//}
					//else
					//{
					//	m_alert.Show( "すでに対象としているフォルダは指定できません" );
					//}
				}
				else
				{
					Destination = dlg.SelectedPath;
					//if( IsDetectExistTarget( info => info.Destination, dlg.SelectedPath ) )
					//{
					//	Destination = dlg.SelectedPath;
					//}
					//else
					//{
					//	m_alert.Show( "すでに対象としているフォルダは指定できません" );
					//}
				}
			}
		}
	}

	private bool IsDetectExistTarget( Func<TargetInformation,string?> transformProc, string? newTarget )
	{
		if( TargetFolderInformation != null && transformProc( TargetFolderInformation ) == newTarget )
		{
			return true;
		}
		// すべての対象と異なる場合はセットしてよい
		return TargetFolderInformationCollection?.Select( transformProc ).All( target => target != newTarget )??true;
	}
	[RelayCommand]
	void OK()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( string.IsNullOrEmpty( Source ) )
		{
			m_alert.Show( "コピー元が指定されていません。" );
			return;
		}
		if( !Directory.Exists( Source ) )
		{
			m_alert.Show( "コピー元フォルダがありません。" );
			return;
		}
		if( !IsDetectExistTarget( info => info.Source, Source ) )
		{
			m_alert.Show( "コピー元フォルダが重複しています" );
			return;
		}
		// コピー先は無くてもよい
		if( string.IsNullOrEmpty( Destination ) )
		{
			m_alert.Show( "コピー先が指定されていません。" );
			return;
		}
		//if( !IsDetectExistTarget( info => info.Destination, Destination ) )
		//{
		//	m_alert.Show( "コピー先フォルダが重複しています" );
		//	return;
		//}
		TargetFolderInformation = new()
		{
			Source = Source,
			Destination = Destination,
		};
		DialogResult = true;
	}

	public TargetInformation TargetFolderInformation { get; set; }
	public ObservableCollection<TargetInformation>? TargetFolderInformationCollection { get; set; }
	public AppendFolderViewModel( ILogger<AppendFolderViewModel> logger, IDispAlert alert )
	{
		m_logger = logger;
		m_alert = alert;
		TargetFolderInformation = new();
	}
	[DesignOnly(true)]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	public AppendFolderViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	{
	}
	private ILogger<AppendFolderViewModel> m_logger;
	private IDispAlert m_alert;
}
