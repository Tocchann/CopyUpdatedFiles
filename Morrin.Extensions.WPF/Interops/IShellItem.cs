using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Morrin.Extensions.WPF.Interops;

/// <summary>
/// IShellItem シェル(エクスプローラ)がファイル等を仮想的にアイテムとして扱うためのインターフェース
/// </summary>
[
	ComImport,
	Guid( "43826D1E-E718-42EE-BC55-A1E261C37BFE" ),
	InterfaceType( ComInterfaceType.InterfaceIsIUnknown )
]
internal interface IShellItem
{
	public enum SIGDN : uint // 今回使う識別子のみ移植
	{
		FILESYSPATH = 0x80058000,
	}
	void BindToHandler(); // BindToHandler 省略
	void GetParent(); // GetParent 省略
	/// <summary>
	/// このオブジェクトの文字列表記を取得
	/// GetDisplayName が本来のメソッド名。今回わざと名前を変更。
	/// </summary>
	/// <param name="sigdnName"></param>
	/// <returns>sigdnName に応じた文字列</returns>
	[return: MarshalAs( UnmanagedType.LPWStr )]
	string GetName( SIGDN sigdnName );
	void GetAttributes();  //  省略
	void Compare();  // Compare 省略
}
