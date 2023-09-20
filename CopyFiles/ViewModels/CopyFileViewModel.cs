using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Contracts.Services;
using CopyFiles.Contracts.Views;
using CopyFiles.Models;
using CopyFiles.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Morrin.Extensions.Abstractions;
using Morrin.Extensions.WPF;
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
using System.Windows.Input;

namespace CopyFiles.ViewModels;

public partial class CopyFileViewModel : ObservableObject, IProgressBarService
{
	public ObservableCollection<TargetInformation> TargetFolderInformationCollection { get; } = new();
	public ObservableCollection<TargetFileInformation> DispTargetFileInformationCollection { get; } = new();

	public ObservableCollection<string> TargetIsmFiles { get; } = new();

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
	bool isHideIgnoreFiles;

	[ObservableProperty]
	bool isDispCopyFilesOnly;

	[ObservableProperty]
	string? selectTargetIsmFile;

	bool CanExecuteIsmFile() => string.IsNullOrEmpty( SelectTargetIsmFile ) == false;
	[RelayCommand]
	void AddIsmFile()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		var dlg = new OpenFileDialog();
		dlg.Filter = "InstallShieldプロジェクト|*.ism|すべてのファイル|*.*";
		if( dlg.ShowDialog() == true )
		{
			if( TargetIsmFiles.Contains( dlg.FileName ) == false )
			{
				// 空データを突っ込んでいるかもしれないので削除する
				TargetIsmFiles.Add( dlg.FileName );
				App.Current.Properties[nameof( TargetIsmFiles )] = TargetIsmFiles;
			}
			else
			{
				m_alert.Show( "同じファイルが指定されています。" );
			}
		}
	}
	[RelayCommand(CanExecute=nameof(CanExecuteIsmFile))]
	void EditIsmFile()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( string.IsNullOrEmpty( SelectTargetIsmFile ) )
		{
			return;
		}
		var dlg = new OpenFileDialog();
		if( dlg.ShowDialog() == true )
		{
			// 違うファイルが選択された場合のみ
			if( string.CompareOrdinal( SelectTargetIsmFile, dlg.FileName ) != 0 )
			{
				if( TargetIsmFiles.Contains( dlg.FileName ) == false )
				{
					TargetIsmFiles.Remove( SelectTargetIsmFile );
					TargetIsmFiles.Add( dlg.FileName );
					SelectTargetIsmFile = dlg.FileName;
					App.Current.Properties[nameof( TargetIsmFiles )] = TargetIsmFiles;
				}
				else
				{
					m_alert.Show( "同じファイルが指定されています。" );
				}
			}
		}
	}
	[RelayCommand(CanExecute=nameof(CanExecuteIsmFile))]
	void DeleteIsmFile()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( string.IsNullOrEmpty( SelectTargetIsmFile ) == false )
		{
			if( m_alert.Show( "選択された InstallShieldプロジェクトファイルを削除します。\nよろしいですか？", IDispAlert.Buttons.YesNo ) != IDispAlert.Result.Yes )
			{
				return;
			}
			TargetIsmFiles.Remove( SelectTargetIsmFile );
			App.Current.Properties[nameof( TargetIsmFiles )] = TargetIsmFiles;
		}
	}
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
	[RelayCommand(CanExecute=nameof( CanExecuteSelectTargetFolderInformation ) )]
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
	bool CanExecuteSelectTargetFolderInformation() => SelectTargetFolderInformation != null;
	[RelayCommand(CanExecute=nameof(CanExecuteSelectTargetFolderInformation))]
	void RemoveFolder()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( SelectTargetFolderInformation != null )
		{
			if( m_alert.Show( "選択された追跡フォルダを削除します。\nよろしいですか？", IDispAlert.Buttons.YesNo ) != IDispAlert.Result.Yes )
			{
				return;
			}
			TargetFolderInformationCollection.Remove( SelectTargetFolderInformation );
			App.Current.Properties[nameof( TargetFolderInformationCollection )] = TargetFolderInformationCollection.ToArray();
		}
	}

	bool CanExecuteTargetFileAction() => DispTargetFileInformationCollection.Count > 0;
	[RelayCommand]
	async Task CheckTargetFiles()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );

		// プログレスバーが出ているかどうかで判定を変える
		if( !IsProgressBarVisible )
		{
			if( !m_tokenSrc.TryReset() )
			{
				m_alert.Show( "キャンセル処理が初期化できません。もう一度試すか、一度アプリを終了してください。" );
			}
			using( var checkTargetFiles = new CheckTargetFiles( this, m_tokenSrc.Token ) )
			{
				await checkTargetFiles.ExecuteAsync( TargetFolderInformationCollection, TargetIsmFiles );
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

	[RelayCommand(CanExecute=nameof(CanExecuteTargetFileAction))]
	async Task CopyTargetFiles()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( CanExecuteTargetFileAction() == false )
		{
			m_alert.Show( "コピーするものがありません。" );
			return;
		}
		if( !IsProgressBarVisible )
		{
			if( !m_tokenSrc.TryReset() )
			{
				m_alert.Show( "キャンセル処理が初期化できません。もう一度試すか、一度アプリを終了してください。" );
			}
			if( m_targetFileInformationCollection != null && m_targetFileInformationCollection.Count != 0 )
			{
				using( var copyTargetFiles = new CopyTargetFiles( this, m_tokenSrc.Token, m_targetFileInformationCollection ) )
				{
					await copyTargetFiles.ExecuteAsync();
				}
			}
			// 本当は再チェックになるけどここではやらない
			m_alert.Show( "コピーが終わりました。" );
		}
		else
		{
			m_tokenSrc.Cancel();
		}

	}


	private void RefreshTargetFileInformationCollection()
	{
		DispTargetFileInformationCollection.Clear();
		// 絞り込み表示するので絞り込んでセットする
		if( m_targetFileInformationCollection != null )
		{
			IEnumerable<TargetFileInformation> collection = m_targetFileInformationCollection;
			if( IsHideIgnoreFiles )
			{
				collection = collection.Where( info => info.Ignore == false );
			}
			if( IsDispCopyFilesOnly )
			{
				collection = collection.Where( info => info.NeedCopy );
			}
			foreach( var fileInfo in collection.OrderBy( info => info.Ignore ).ThenBy( info => info.Status ).ThenBy( info => info.Source ) )
			{
				DispTargetFileInformationCollection.Add( fileInfo );
			}
		}
		CopyTargetFilesCommand?.NotifyCanExecuteChanged();
	}
	protected override void OnPropertyChanged( PropertyChangedEventArgs e )
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
		case nameof( IsHideIgnoreFiles ):
			App.Current.Properties[nameof( IsHideIgnoreFiles )] = IsHideIgnoreFiles;
			RefreshTargetFileInformationCollection();
			break;
		case nameof( SelectTargetIsmFile ):
			EditIsmFileCommand.NotifyCanExecuteChanged();
			DeleteIsmFileCommand.NotifyCanExecuteChanged();
			break;
		}
		base.OnPropertyChanged( e );
	}

	public CopyFileViewModel( ILogger<CopyFileViewModel> logger, IDispAlert alart )
	{
		m_logger = logger;
		m_alert = alart;
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
		// ここは直接boolが格納されているので、そのまま変換する
		IsDispCopyFilesOnly = ((JsonElement?)App.Current.Properties[nameof( IsDispCopyFilesOnly )])?.GetBoolean() ?? false;
		IsHideIgnoreFiles = ((JsonElement?)App.Current.Properties[nameof( IsHideIgnoreFiles )])?.GetBoolean() ?? false;
		if( App.Current.Properties.Contains( nameof( TargetIsmFiles ) ) )
		{
			var jsonElement = (JsonElement?)App.Current.Properties[nameof( TargetIsmFiles )];
			if( jsonElement != null )
			{
				var ismFiles = JsonSerializer.Deserialize<string[]>( jsonElement.Value );
				if( ismFiles != null )
				{
					foreach( var file in ismFiles )
					{
						TargetIsmFiles.Add( file );
					}
				}
			}
		}
		// 無くなったので削除
		if( App.Current.Properties.Contains( "FocusFileListPath" ) )
		{
			App.Current.Properties.Remove( "FocusFileListPath" );
		}

		RefreshTargetFileInformationCollection();
		m_tokenSrc = new();
	}

	[DesignOnly( true )]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	public CopyFileViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	{
	}
	private List<TargetFileInformation>? m_targetFileInformationCollection;
	private ILogger<CopyFileViewModel> m_logger;
	private IDispAlert m_alert;
	private CancellationTokenSource m_tokenSrc;
	private object m_progressValueLocker;
}
