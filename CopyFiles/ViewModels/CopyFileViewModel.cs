using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Contracts.Services;
using CopyFiles.Contracts.Views;
using CopyFiles.Models;
using CopyFiles.Services;
using Microsoft.Extensions.Logging;
using Morrin.Extensions.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CopyFiles.ViewModels;

public partial class CopyFileViewModel : ObservableObject, IProgressBarService
{
	public ObservableCollection<TargetInformation> TargetFolderInformationCollection { get; }
	public ObservableCollection<TargetFileInformation> TargetFileInformationCollection { get; }

	[ObservableProperty]
	TargetInformation? selectTargetFolderInformation;

	[ObservableProperty]
	bool isProgressBarVisible;

	[ObservableProperty]
	bool isIndeterminate;

	[ObservableProperty]
	int progressMin;

	[ObservableProperty]
	int progressMax;

	[ObservableProperty]
	int progressValue;

	[RelayCommand]
	void AddFolder()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		var dlg = App.Current.GetService<IAppendFolderDialog>();
		if( dlg != null )
		{
			dlg.ViewModel.DialogTitle = "追跡フォルダの追加";
			dlg.ViewModel.TargetFolderInformationCollection = TargetFolderInformationCollection;
			if( dlg.ShowWindow() != false )
			{
				TargetFolderInformationCollection.Add( dlg.ViewModel.TargetFolderInformation );
				var cols = (List<TargetInformation>?)App.Current.Properties["TargetFolderInformations"];
				if( cols == null )
				{
					cols = new();
					App.Current.Properties["TargetFolderInformations"] = cols;
				}
				cols.Add( dlg.ViewModel.TargetFolderInformation );
			}
		}
	}
	[RelayCommand(CanExecute =nameof( CanExecuteSelectTargetFolderInformation ) )]
	void EditFolder()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		var dlg = App.Current.GetService<IAppendFolderDialog>();
		if( dlg != null && SelectTargetFolderInformation != null )
		{
			dlg.ViewModel.DialogTitle = "追跡フォルダの編集";
			dlg.ViewModel.TargetFolderInformation = SelectTargetFolderInformation;
			dlg.ViewModel.Source = SelectTargetFolderInformation.Source;
			dlg.ViewModel.Destination = SelectTargetFolderInformation.Destination;
			dlg.ViewModel.TargetFolderInformationCollection = TargetFolderInformationCollection;
			if( dlg.ShowWindow() != false )
			{
				var cols = (List<TargetInformation>?)App.Current.Properties["TargetFolderInformations"];
				if( cols != null )
				{
					var info = cols.Single( info => info.Source == SelectTargetFolderInformation.Source && info.Destination == SelectTargetFolderInformation.Destination );
					info.Source = dlg.ViewModel.Source;
					info.Destination = dlg.ViewModel.Destination;
				}
				SelectTargetFolderInformation.Source = dlg.ViewModel.Source;
				SelectTargetFolderInformation.Destination = dlg.ViewModel.Destination;
			}
		}
	}
	bool CanExecuteSelectTargetFolderInformation()
	{
		return SelectTargetFolderInformation != null;
	}
	[RelayCommand(CanExecute =nameof(CanExecuteSelectTargetFolderInformation))]
	void RemoveFolder()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( SelectTargetFolderInformation != null )
		{
			if( m_alart.Show( "選択された追跡フォルダを削除します。\nよろしいですか？", IDispAlert.Buttons.YesNo ) != IDispAlert.Result.Yes )
			{
				return;
			}
			var cols = (List<TargetInformation>?)App.Current.Properties["TargetFolderInformations"];
			if( cols != null )
			{
				cols.Remove( SelectTargetFolderInformation );
			}
			TargetFolderInformationCollection.Remove( SelectTargetFolderInformation );
		}
	}

	[RelayCommand(CanExecute =nameof(CanExecuteTargetFileAction))]
	void CopyToClipboard()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		m_alart.Show( "工事中...クリップボードへのコピー" );
	}
	bool CanExecuteTargetFileAction()
	{
		return TargetFileInformationCollection.Count > 0;
	}
	[RelayCommand]
	async Task CheckTargetFiles()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );

		// プログレスバーが出ているかどうかで判定を変える
		if( !IsProgressBarVisible )
		{
			using( var checkTargetFiles = new CheckTargetFiles( TargetFileInformationCollection, this, m_tokenSrc.Token ) )
			{
				await checkTargetFiles.ExecuteAsync( TargetFolderInformationCollection );
				// データを構築し終わったらコピーする(ここはメインスレッドでも良い)
				TargetFileInformationCollection.Clear();
				foreach( var fileInfo in checkTargetFiles.TargetFileInfos.OrderBy( info => info.Status ) )
				{
					TargetFileInformationCollection.Add( fileInfo );
				}
			}
			// 処理が終わったら、チェックリストが変わっているので、コピーとかが可能になる(はず)
			CopyToClipboardCommand.NotifyCanExecuteChanged();
			CopyTargetFilesCommand.NotifyCanExecuteChanged();
		}
		else
		{
			// キャンセルを押す(何度押しても良い)
			m_tokenSrc.Cancel();
		}
	}

	[RelayCommand(CanExecute =nameof(CanExecuteTargetFileAction))]
	void CopyTargetFiles()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		m_alart.Show( "工事中...追跡対象ファイルのコピー" );
	}
	public CopyFileViewModel( ILogger<CopyFileViewModel> logger, IDispAlert alart )
	{
		m_logger = logger;
		m_alart = alart;
		TargetFolderInformationCollection = new();
		if( App.Current.Properties.Contains( "TargetFolderInformations" ) )
		{
			var cols = (List<TargetInformation>?)App.Current.Properties["TargetFolderInformations"];
			if( cols != null )
			{
				TargetFolderInformationCollection.Clear();
				foreach( var info in cols )
				{
					TargetFolderInformationCollection.Add( info );
				}
			}
		}
		TargetFileInformationCollection = new();
		m_tokenSrc = new();
		PropertyChanged += ( s, e ) =>
		{
			switch( e.PropertyName )
			{
			case nameof( SelectTargetFolderInformation ):
				EditFolderCommand.NotifyCanExecuteChanged();
				RemoveFolderCommand.NotifyCanExecuteChanged();
				break;
			}
		};
	}
	[DesignOnly( true )]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	public CopyFileViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	{
	}
	private ILogger<CopyFileViewModel> m_logger;
	private IDispAlert m_alart;
	private CancellationTokenSource m_tokenSrc;
}
