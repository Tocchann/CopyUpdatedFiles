using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Morrin.Extensions.Abstractions;
using Morrin.Extensions.WPF.Interops;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using static Morrin.Extensions.WPF.Interops.IFileOpenDialog;
using static Morrin.Extensions.WPF.Interops.IShellItem;
using System.Collections;

namespace Morrin.Extensions.WPF;

public class SelectFolderDialog : ISelectFolderDialog
{
	public static IServiceCollection ConfigureServices( IServiceCollection services )
	{
		services.AddSingleton<ISelectFolderDialog, SelectFolderDialog>();
		return services;
	}

	public string? InitialFolder { get; set; }
	public string? SelectedPath {get; set; }
	public string? Title { get; set; }

	public void AddPlace( string folder, ISelectFolderDialog.FDAP fdap )
	{
		m_places.Add( (folder, fdap) );
	}

	public bool? ShowDialog()
	{
		var ownerWindow = Utilities.GetOwnerWindow();
		return ShowDialog( NativeMethods.GetSafeOwnerWindow( ownerWindow ) );
	}
	private bool? ShowDialog( IntPtr ownerWindow )
	{
		IFileOpenDialog? dlg = new FileOpenDialog() as IFileOpenDialog;  //	IUnknown::QueryInterfaceを使ってインターフェースを特定する
		if( dlg != null )
		{
			try
			{
				//	フォルダ選択モードに切り替え
				dlg.SetOptions( FOS.FORCEFILESYSTEM | FOS.PICKFOLDERS );
				//	以前選択されていたフォルダを指定
				bool setFolder = false;
				var item = CreateItem( SelectedPath );
				if( item is not null )
				{
					dlg.SetFolder( item );
					Marshal.ReleaseComObject( item );
					setFolder = true;
				}
				//	まだフォルダを設定していない場合は初期フォルダを設定する
				if( !setFolder )
				{
					item = CreateItem( InitialFolder );
					if( item is not null )
					{
						dlg.SetFolder( item );
						Marshal.ReleaseComObject( item );
					}
				}
				//	タイトル
				if( !string.IsNullOrWhiteSpace( Title ) )
				{
					dlg.SetTitle( Title );
				}
				//	ショートカット追加
				foreach( var place in m_places )
				{
					item = CreateItem( place.folder );
					if( item is not null )
					{
						dlg.AddPlace( item, (IFileOpenDialog.FDAP)place.fdap );
						Marshal.ReleaseComObject( item );
					}
				}
				//	ダイアログを表示
				var hRes = dlg.Show( ownerWindow );
				if( NativeMethods.SUCCEEDED( hRes ) )
				{
					item = dlg.GetResult();
					SelectedPath = item.GetName( SIGDN.FILESYSPATH );
					Marshal.ReleaseComObject( item );
					return true;
				}
				// キャンセル以外のエラーが来た場合はなにかしら問題ありなので例外を投げる
				else if( hRes != NativeMethods.HRESULT_FROM_WIN32(NativeMethods.Win32Error.Cancelled) )
				{
					// ここは、正直例外を投げたほうがいいと思うがどうなんだろう？
					throw new COMException( "IFileOpenDialog.Show()のエラー", hRes );
				}
				return false;
			}
			finally
			{
				Marshal.FinalReleaseComObject( dlg );
			}
		}
		return null;	//	ダイアログが用意できない場合(ここでは例外を投げない)
	}
	public SelectFolderDialog( ILogger<SelectFolderDialog> logger )
	{
		m_logger = logger;
		m_places = new List<(string folder, ISelectFolderDialog.FDAP fdap)>();
	}
	/// <summary>
	/// SHCreateItemFromParseName() のラッパー。
	/// ファイルパスから、IShellItem を作成する専用メソッドとして用意。
	/// </summary>
	private static IShellItem? CreateItem( string? folder )
	{
		if( !string.IsNullOrWhiteSpace( folder ) &&
			NativeMethods.SUCCEEDED(
				NativeMethods.SHCreateItemFromParsingName( folder,
					IntPtr.Zero, typeof( IShellItem ).GUID, out var item ) ) )
		{
			return item;
		}
		return null;
	}
	private List<(string folder, ISelectFolderDialog.FDAP fdap)> m_places;
	private ILogger<SelectFolderDialog> m_logger;
}
