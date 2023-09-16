using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Morrin.Extensions.WPF.Interops;

internal static partial class NativeMethods
{
	// TaskDialog
	public static nint MAKEINTRESOURCE( int resId )
	{
		//#define MAKEINTRESOURCEW(i) ((LPWSTR)((ULONG_PTR)((WORD)(i))))
		return new nint( (ushort)(resId) );
	}
	[Flags]
	public enum TaskDialogCommonButtonFlags
	{
		Ok = 0x0001, // selected control return value IDOK
		Yes = 0x0002, // selected control return value IDYES
		No = 0x0004, // selected control return value IDNO
		Cancel = 0x0008, // selected control return value IDCANCEL
						 // WPFのMessageBoxでは利用しないので定義もいれない
						 //Retry = 0x0010, // selected control return value IDRETRY
						 //Close = 0x0020  // selected control return value IDCLOSE
	}
	public enum TaskDialogResult : int
	{
		None = 0,
		OK = 1,
		Cancel = 2,
		Yes = 6,
		No = 7
	}
	[DllImport( "Comctl32.dll", CharSet = CharSet.Unicode, PreserveSig = false )]
	public static extern void TaskDialog(
		IntPtr hwndOwner, IntPtr hInstance,
		string pszWindowTitle, string pszMainInstruction, string pszContent,
		TaskDialogCommonButtonFlags dwCommonButtons, IntPtr pszIcon, out TaskDialogResult pnButton );
}
