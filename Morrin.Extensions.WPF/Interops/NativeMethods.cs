using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Morrin.Extensions.Abstractions.IDispAlert;

namespace Morrin.Extensions.WPF.Interops;

internal static class NativeMethods
{
	/// <summary>
	/// hwndOwner で指定されたウィンドウをオーナーウィンドウとして指定できるウィンドウに正規化する
	/// </summary>
	/// <param name="hwndOwner"></param>
	/// <returns></returns>
	public static IntPtr GetSafeOwnerWindow( IntPtr hwndOwner )
	{
		//	無効なウィンドウを参照している場合の排除
		if( hwndOwner != IntPtr.Zero && !IsWindow( hwndOwner ) )
		{
			hwndOwner = IntPtr.Zero;
		}
		//	オーナーウィンドウの基本を探す
		if( hwndOwner == IntPtr.Zero )
		{
			hwndOwner = GetForegroundWindow();
		}
		//	トップレベルウィンドウを探す
		IntPtr hwndParent = hwndOwner;
		while( hwndParent != IntPtr.Zero )
		{
			hwndOwner = hwndParent;
			hwndParent = GetParent( hwndOwner );
		}
		//	トップレベルウィンドウに所属する現在アクティブなポップアップ(自分も含む)を取得
		if( hwndOwner != IntPtr.Zero )
		{
			hwndOwner = GetLastActivePopup( hwndOwner );
		}
		return hwndOwner;
	}

	//	HWND サポート
	[DllImport( "user32.dll" )]
	[return: MarshalAs( UnmanagedType.Bool )]
	public static extern bool IsWindow( IntPtr hWnd );
	[DllImport( "user32.dll" )]
	public static extern IntPtr GetForegroundWindow();
	[DllImport( "user32.dll" )]
	public static extern IntPtr GetParent( IntPtr hwnd );
	[DllImport( "user32.dll" )]
	public static extern IntPtr GetLastActivePopup( IntPtr hwnd );
	//	Shell サポート(例外処理をしたくないので、HRESULT を受け取る)
	[DllImport( "shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true )]
	public static extern int SHCreateItemFromParsingName(
		[In][MarshalAs( UnmanagedType.LPWStr )] string pszPath,
		[In] IntPtr pbc,
		[In][MarshalAs( UnmanagedType.LPStruct )] Guid riid,
		[Out][MarshalAs( UnmanagedType.Interface, IidParameterIndex = 2 )] out IShellItem ppv );
	//	HRESULT サポート
	public static bool SUCCEEDED( int result ) => result >= 0;
	public static bool FAILED( int result ) => result < 0;
	public static int HRESULT_FROM_WIN32( int result ) =>
		result <= 0 ? result : (int)(0x80000000 | (int)(result & 0xFFFF) | (FACILITY_WIN32 << 16));
	public static int HRESULT_FROM_WIN32( Win32Error result ) =>
		(int)(0x80000000 | ((int)result & 0xFFFF) | (FACILITY_WIN32 << 16));

	static uint FACILITY_WIN32 = 7;
	public enum Win32Error : int
	{
		Success,
		Cancelled=1223,
	}
}
