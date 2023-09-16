using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Morrin.Extensions.WPF.Interops;

internal static partial class NativeMethods
{
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
