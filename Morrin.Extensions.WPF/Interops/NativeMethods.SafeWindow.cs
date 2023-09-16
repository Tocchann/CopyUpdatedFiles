using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Morrin.Extensions.WPF.Interops;

internal static partial class NativeMethods
{
	/// <summary>
	/// hwndOwner で指定されたウィンドウをオーナーウィンドウとして指定できるウィンドウに正規化する
	/// </summary>
	/// <param name="hwndOwner"></param>
	/// <returns></returns>
	public static IntPtr GetSafeOwnerWindow( Window? ownerWindow )
	{
		IntPtr hwndOwner = IntPtr.Zero;
		if( ownerWindow != null )
		{
			var hwndSrc = System.Windows.Interop.HwndSource.FromVisual( ownerWindow ) as System.Windows.Interop.HwndSource;
			hwndOwner = hwndSrc?.Handle ?? IntPtr.Zero;
		}
		return NativeMethods.GetSafeOwnerWindow( hwndOwner );
	}
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
}
