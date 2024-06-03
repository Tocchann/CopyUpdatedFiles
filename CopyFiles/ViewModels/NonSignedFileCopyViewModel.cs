using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Contracts.Services;
using CopyFiles.Contracts.Views;
using CopyFiles.Models;
using CopyFiles.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Morrin.Extensions.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CopyFiles.ViewModels;

public partial class NonSignedFileCopyViewModel : ImplementProgressBarBaseViewModel
{
	public ObservableCollection<string> TargetIsmFiles { get; } = new();
	public ObservableCollection<TargetInformation> UnsignedFolderCollection { get; } = new();
	public ObservableCollection<TargetFileInformation> DispTargetFileInformationCollection { get; } = new();

	[ObservableProperty]
	TargetInformation? selectUnsignedFolder;

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
		dlg.Filter = "InstallShieldプロジェクト|*.ism|実行ファイル|*.exe;*.dll|すべてのファイル|*.*";
		if( dlg.ShowDialog() == true )
		{
			if( TargetIsmFiles.Contains( dlg.FileName ) == false )
			{
				// 空データを突っ込んでいるかもしれないので削除する
				TargetIsmFiles.Add( dlg.FileName );
				App.Current.CurrentTargetSolution.TargetIsmFiles = TargetIsmFiles.ToArray();
			}
			else
			{
				m_alert.Show( "同じファイルが指定されています。" );
			}
		}
	}
	[RelayCommand( CanExecute = nameof( CanExecuteIsmFile ) )]
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
					App.Current.CurrentTargetSolution.TargetIsmFiles = TargetIsmFiles.ToArray();
				}
				else
				{
					m_alert.Show( "同じファイルが指定されています。" );
				}
			}
		}
	}
	[RelayCommand( CanExecute = nameof( CanExecuteIsmFile ) )]
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
			App.Current.CurrentTargetSolution.TargetIsmFiles = TargetIsmFiles.ToArray();
		}
	}

	bool CanExecuteUnsignedFolderAction() => SelectUnsignedFolder != null;
	[RelayCommand]
	void AddUnsignedFolder()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		var dlg = App.Current.GetService<IAppendFolderDialog>();
		if( dlg != null )
		{
			dlg.ViewModel.DialogTitle = "非署名ファイルのコピー対象フォルダの追加";
			dlg.ViewModel.TargetFolderInformationCollection = UnsignedFolderCollection;
			if( dlg.ShowWindow() != false )
			{
				UnsignedFolderCollection.Add( dlg.ViewModel.TargetFolderInformation );
				App.Current.CurrentTargetSolution.NonSignedFileCopyDataModel.TargetInformations = UnsignedFolderCollection.ToArray();

				CheckTargetFilesCommand.NotifyCanExecuteChanged();
			}
		}
	}
	[RelayCommand( CanExecute = nameof( CanExecuteUnsignedFolderAction ) )]
	void EditUnsignedFolder()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		var dlg = App.Current.GetService<IAppendFolderDialog>();
		if( dlg != null && SelectUnsignedFolder != null )
		{
			dlg.ViewModel.DialogTitle = "非署名ファイルのコピー対象フォルダの編集";
			dlg.ViewModel.TargetFolderInformation = SelectUnsignedFolder;
			dlg.ViewModel.Source = SelectUnsignedFolder.Source;
			dlg.ViewModel.Destination = SelectUnsignedFolder.Destination;
			dlg.ViewModel.TargetFolderInformationCollection = UnsignedFolderCollection;
			if( dlg.ShowWindow() != false )
			{
				SelectUnsignedFolder.Source = dlg.ViewModel.Source;
				SelectUnsignedFolder.Destination = dlg.ViewModel.Destination;
				// 実際は、コレクションデータも変わるのでそのまま書き込んでおけばよい
				App.Current.CurrentTargetSolution.NonSignedFileCopyDataModel.TargetInformations = UnsignedFolderCollection.ToArray();

				CheckTargetFilesCommand.NotifyCanExecuteChanged();
			}
		}
	}
	[RelayCommand( CanExecute = nameof( CanExecuteUnsignedFolderAction ) )]
	void RemoveUnsignedFolder()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( SelectUnsignedFolder != null )
		{
			if( m_alert.Show( "選択された非署名ファイルのコピー対象フォルダを削除します。\nよろしいですか？", IDispAlert.Buttons.YesNo ) != IDispAlert.Result.Yes )
			{
				return;
			}
			UnsignedFolderCollection.Remove( SelectUnsignedFolder );
			App.Current.CurrentTargetSolution.NonSignedFileCopyDataModel.TargetInformations = UnsignedFolderCollection.ToArray();

			CheckTargetFilesCommand.NotifyCanExecuteChanged();
		}
	}

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
				await checkTargetFiles.ExecuteAsync( false, UnsignedFolderCollection, TargetIsmFiles );
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

	bool CanExecuteTargetFileAction() => DispTargetFileInformationCollection.Count > 0;
	[RelayCommand( CanExecute = nameof( CanExecuteTargetFileAction ) )]
	async Task CopyUnsignedFiles()
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( UnsignedFolderCollection.Count == 0 )
		{
			m_alert.Show( "処理対象フォルダが確定していません" );
		}
		if( !IsProgressBarVisible )
		{
			if( !m_tokenSrc.TryReset() )
			{
				m_alert.Show( "キャンセル処理が初期化できません。もう一度試すか、一度アプリを終了してください。" );
			}
			if( m_targetFileInformationCollection != null && m_targetFileInformationCollection.Count != 0 )
			{
				using( var copyTargetFiles = new CopyTargetFiles( this, m_tokenSrc.Token ) )
				{
					await copyTargetFiles.ExecuteAsync( m_targetFileInformationCollection );
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

	partial void OnSelectUnsignedFolderChanged( TargetInformation? value )
	{
		EditUnsignedFolderCommand.NotifyCanExecuteChanged();
		RemoveUnsignedFolderCommand.NotifyCanExecuteChanged();
	}
	partial void OnIsDispCopyFilesOnlyChanged( bool value )
	{
		App.Current.CurrentTargetSolution.NonSignedFileCopyDataModel.IsDispCopyFilesOnly = value;
		RefreshTargetFileInformationCollection();
	}
	partial void OnIsHideIgnoreFilesChanged( bool value )
	{
		App.Current.CurrentTargetSolution.NonSignedFileCopyDataModel.IsHideIgnoreFiles = value;
		RefreshTargetFileInformationCollection();
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
			// 表示上のコピー対象はNeedCopyで判断する最終的なコピー対象から除外できるように、実際のコピーは手動でセットすることにした
			if( IsDispCopyFilesOnly )
			{
				collection = collection.Where( info => info.NeedCopy );
			}
			foreach( var fileInfo in collection.OrderBy( info => info.Ignore ).ThenBy( info => info.Status ).ThenBy( info => info.Source ) )
			{
				DispTargetFileInformationCollection.Add( fileInfo );
			}
		}
		CopyUnsignedFilesCommand.NotifyCanExecuteChanged();
	}

	public NonSignedFileCopyViewModel( ILogger<NonSignedFileCopyViewModel> logger, IDispAlert alert )
	{
		m_tokenSrc = new();
		m_logger = logger;
		m_alert = alert;
		UnsignedFolderCollection.Clear();
		foreach( var info in App.Current.CurrentTargetSolution.NonSignedFileCopyDataModel.TargetInformations )
		{
			UnsignedFolderCollection.Add( info );
		}
		foreach( var ismFile in App.Current.CurrentTargetSolution.TargetIsmFiles )
		{
			TargetIsmFiles.Add( ismFile );
		}

		IsDispCopyFilesOnly = App.Current.CurrentTargetSolution.NonSignedFileCopyDataModel.IsDispCopyFilesOnly;
		IsHideIgnoreFiles = App.Current.CurrentTargetSolution.NonSignedFileCopyDataModel.IsHideIgnoreFiles;

		RefreshTargetFileInformationCollection();
	}
	[DesignOnly( true )]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	public NonSignedFileCopyViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	{
	}
	private ILogger<NonSignedFileCopyViewModel> m_logger;
	private IDispAlert m_alert;
	private CancellationTokenSource m_tokenSrc;
	private List<TargetFileInformation>? m_targetFileInformationCollection;
}
