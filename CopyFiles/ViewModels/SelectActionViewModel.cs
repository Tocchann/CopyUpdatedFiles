using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Contracts.Views;
using CopyFiles.Models;
using Microsoft.Extensions.Logging;
using Morrin.Extensions.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CopyFiles.ViewModels;

public partial class SelectActionViewModel : ObservableObject
{
	[RelayCommand]
	void CopyBuildFiles()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( string.IsNullOrWhiteSpace( SelectedSolution ) )
		{
			m_alert.Show( "ソリューションが選択されていません", IDispAlert.Buttons.OK, IDispAlert.Icon.Asterisk );
			return;
		}
		App.Current.Properties.TargetSolution = SelectedSolution;
		if( !App.Current.Properties.TargetSolutions.ContainsKey( SelectedSolution ) )
		{
			App.Current.Properties.TargetSolutions[SelectedSolution] = new();
		}
		var dlg = App.Current.GetService<ICopyFileView>();
		dlg?.ShowWindow();
	}

	[RelayCommand]
	void CopyNonSignedFiles()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( string.IsNullOrWhiteSpace( SelectedSolution ) )
		{
			m_alert.Show( "ソリューションが選択されていません", IDispAlert.Buttons.OK, IDispAlert.Icon.Asterisk );
			return;
		}
		App.Current.Properties.TargetSolution = SelectedSolution;
		if( !App.Current.Properties.TargetSolutions.ContainsKey( SelectedSolution ) )
		{
			App.Current.Properties.TargetSolutions[SelectedSolution] = new();
		}
		var dlg = App.Current.GetService<INonSignedFileCopyView>();
		dlg?.ShowWindow();
	}
	public ObservableCollection<string> TargetSolutions { get; } = new();

	[ObservableProperty]
	string? selectedSolution;

	partial void OnSelectedSolutionChanged( string? value )
	{
		App.Current.Properties.TargetSolution = value ?? string.Empty;
	}
	[RelayCommand]
	void AddSolution()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		var dlg = App.Current.GetService<IAddSolutionDialog>();
		if( dlg != null )
		{
			dlg.ViewModel.TargetSolutions = TargetSolutions.ToArray();
			if( dlg.ShowWindow() == true )
			{
				TargetSolutions.Add( dlg.ViewModel.SolutionName );
				SelectedSolution = dlg.ViewModel.SolutionName;
				App.Current.Properties.TargetSolutions[SelectedSolution] = new();
				App.Current.Properties.TargetSolution = SelectedSolution;
			}
		}
	}
	[RelayCommand]
	void RemoveSolution()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( !string.IsNullOrWhiteSpace( SelectedSolution ) )
		{
			if( m_alert.Show( "選択されたソリューションを削除します。\nよろしいですか？", IDispAlert.Buttons.YesNo ) != IDispAlert.Result.Yes )
			{
				return;
			}
			TargetSolutions.Remove( SelectedSolution );
			App.Current.Properties.TargetSolutions.Remove( SelectedSolution );
			SelectedSolution = null;
			App.Current.Properties.TargetSolution = string.Empty;
		}
	}
	public SelectActionViewModel( ILogger<SelectActionViewModel> logger, IDispAlert alert )
	{
		m_logger = logger;
		m_alert = alert;
		TargetSolutions.Clear();
		foreach( var solution in App.Current.Properties.TargetSolutions )
		{
			TargetSolutions.Add( solution.Key );	//	ここではキー名だけ有ればよい
		}
		SelectedSolution = App.Current.Properties.TargetSolution;
		if( !TargetSolutions.Contains( SelectedSolution ) )
		{
			TargetSolutions.Add( SelectedSolution );
		}
	}

	[DesignOnly( true )]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	public SelectActionViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	{
	}
	private ILogger<SelectActionViewModel> m_logger;
	private IDispAlert m_alert;
}
