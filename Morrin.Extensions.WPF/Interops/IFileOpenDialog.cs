using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Morrin.Extensions.WPF.Interops;

//	ファイルダイアログ関連定義
/// <summary>
/// coclass FileOpenDialog
/// </summary>
[ComImport, Guid( "DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7" )] public class FileOpenDialog { }
[
	ComImport,
	Guid( "42f85136-db7e-439c-85f1-e4075d135fc8" ),
	InterfaceType( ComInterfaceType.InterfaceIsIUnknown )
]
internal interface IFileOpenDialog
{
	/// <summary>
	/// enum FOS(_FILEOPENDIALOGOPTIONSの略称)
	/// enum 名は、C/C++ 定義の略称名を利用。
	/// 今回使用するファイルシステム限定フラグ(フォルダの指定がメインなので)と
	/// フォルダ選択モードのフラグのみ転写。
	/// </summary>
	[Flags]
	public enum FOS
	{
		FORCEFILESYSTEM = 0x40,
		PICKFOLDERS = 0x20,
	}
	public enum FDAP
	{
		BOTTOM = 0,
		TOP = 1
	}

	//	IModalWindow
	[PreserveSig]
	int Show( IntPtr hwndParent );
	//	IFileDialog
	void SetFileTypes();
	void SetFileTypeIndex();
	void GetFileTypeIndex();
	void Advise();
	void Unadvise();
	void SetOptions( FOS fos );
	void GetOptions();
	void SetDefaultFolder();
	void SetFolder( IShellItem psi );
	void GetFolder();
	void GetCurrentSelection();
	void SetFileName();
	void GetFileName();
	void SetTitle( [MarshalAs( UnmanagedType.LPWStr )] string pszTitle );
	void SetOkButtonLabel();
	void SetFileNameLabel();
	IShellItem GetResult();
	void AddPlace( IShellItem item, FDAP fdap );
	void SetDefaultExtension();
	void Close();
	void SetClientGuid();
	void ClearClientData();
	void SetFilter();
	//	IFileOpenDialog
	void GetResults();
	void GetSelectedItems();
}
