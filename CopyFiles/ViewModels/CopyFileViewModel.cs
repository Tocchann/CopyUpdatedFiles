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
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CopyFiles.ViewModels;

public partial class CopyFileViewModel : ObservableObject, IProgressBarService
{
	public ObservableCollection<TargetInformation> TargetFolderInformationCollection { get; }
	public ObservableCollection<TargetFileInformation> DispTargetFileInformationCollection { get; }

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

	[ObservableProperty]
	bool isDispCopyFilesOnly;

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
				App.Current.Properties[nameof( TargetFolderInformationCollection )] = TargetFolderInformationCollection.ToArray();
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
				SelectTargetFolderInformation.Source = dlg.ViewModel.Source;
				SelectTargetFolderInformation.Destination = dlg.ViewModel.Destination;
				// 実際は、コレクションデータも変わるのでそのまま書き込んでおけばよい
				App.Current.Properties[nameof( TargetFolderInformationCollection )] = TargetFolderInformationCollection.ToArray();
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
			TargetFolderInformationCollection.Remove( SelectTargetFolderInformation );
			App.Current.Properties[nameof( TargetFolderInformationCollection )] = TargetFolderInformationCollection.ToArray();
		}
	}

	[RelayCommand(CanExecute =nameof(CanExecuteTargetFileAction))]
	void CopyToClipboard()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		// データ形式を決めないといけないよね…テキストで処理することは確定事項だけども…
		m_alart.Show( "工事中...クリップボードへのコピー" );
	}
	bool CanExecuteTargetFileAction()
	{
		// 非表示のものは処理対象に含めない
		return DispTargetFileInformationCollection.Count > 0;
	}
	[RelayCommand]
	async Task CheckTargetFiles()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );

		// プログレスバーが出ているかどうかで判定を変える
		if( !IsProgressBarVisible )
		{
			using( var checkTargetFiles = new CheckTargetFiles( this, m_tokenSrc.Token ) )
			{
				await checkTargetFiles.ExecuteAsync( TargetFolderInformationCollection );
				// データを構築し終わったらコピーする(ここはメインスレッドで良い)
				m_targetFileInformationCollection = checkTargetFiles.TargetFileInfos;
			}
			RefreshTargetFileInformationCollection();
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


	private void RefreshTargetFileInformationCollection()
	{
		DispTargetFileInformationCollection.Clear();
		// 絞り込み表示するので絞り込んでセットする
		if( m_targetFileInformationCollection != null )
		{
			if( IsDispCopyFilesOnly )
			{
				foreach( var fileInfo in m_targetFileInformationCollection )
				{
					if( !fileInfo.Ignore == false )
					{
						if( fileInfo.Status == TargetStatus.NotExist || fileInfo.Status == TargetStatus.Different )
						{
							DispTargetFileInformationCollection.Add( fileInfo );
						}
					}
				}
			}
			else
			{
				// 全面表示は無条件に追加
				foreach( var fileInfo in m_targetFileInformationCollection )
				{
					DispTargetFileInformationCollection.Add( fileInfo );
				}
			}
		}
		CopyToClipboardCommand.NotifyCanExecuteChanged();
		CopyTargetFilesCommand.NotifyCanExecuteChanged();
	}

	public CopyFileViewModel( ILogger<CopyFileViewModel> logger, IDispAlert alart )
	{
		m_logger = logger;
		m_alart = alart;
		TargetFolderInformationCollection = new();
		m_progressValueLocker = new();
		// ここで読み込むときだけ状況が異なる
		if( App.Current.Properties.Contains( nameof( TargetFolderInformationCollection ) ) )
		{
			// 読み取った時は、JsonElementになっているので変換してやる必要がある(本当は型を見て処理するほうがいいけどここでは省略)
			var jsonElement = (JsonElement?)App.Current.Properties[nameof( TargetFolderInformationCollection )];
			if( jsonElement != null )
			{
				var folderInfos = JsonSerializer.Deserialize<TargetInformation[]>( jsonElement.Value );
				if( folderInfos != null )
				{
					foreach( var info in folderInfos )
					{
						TargetFolderInformationCollection.Add( info );
					}
				}
			}
		}
		DispTargetFileInformationCollection = new();
		// ここは直接boolが格納されているので、そのまま変換する
		IsDispCopyFilesOnly = ((JsonElement?)App.Current.Properties[nameof( IsDispCopyFilesOnly )])?.GetBoolean() ?? false;
		RefreshTargetFileInformationCollection();
		m_tokenSrc = new();
		PropertyChanged += ( s, e ) =>
		{
			switch( e.PropertyName )
			{
			case nameof( SelectTargetFolderInformation ):
				EditFolderCommand.NotifyCanExecuteChanged();
				RemoveFolderCommand.NotifyCanExecuteChanged();
				break;
			case nameof( IsDispCopyFilesOnly ):
				App.Current.Properties[nameof( IsDispCopyFilesOnly )] = IsDispCopyFilesOnly;
				RefreshTargetFileInformationCollection();
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
	private List<TargetFileInformation>? m_targetFileInformationCollection;
	private ILogger<CopyFileViewModel> m_logger;
	private IDispAlert m_alart;
	private CancellationTokenSource m_tokenSrc;
	private object m_progressValueLocker;
}
