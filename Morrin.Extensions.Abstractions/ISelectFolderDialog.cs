using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morrin.Extensions.Abstractions
{
	/// <summary>
	/// .NET 8までのつなぎ版
	/// インターフェース自体は残しておいてもいいが、AddPlace は無くなる可能性が高い
	/// </summary>
	public interface ISelectFolderDialog
	{
		public enum FDAP
		{
			BOTTOM = 0,
			TOP = 1
		}
		/// <summary>
		/// 初期フォルダの指定(省略可)
		/// SelectedPath がセットされていない(or設定がおかしい)等の場合に利用される
		/// </summary>
		public string? InitialFolder { get; set; }
		/// <summary>
		/// set = 前回設定していたパスの指定(省略可)
		/// get = ダイアログで選択したパス(Cancel終了の場合は設定されない)
		/// </summary>
		public string? SelectedPath { get; set; }
		/// <summary>
		/// ダイアログのタイトル省略時は、「開く」
		/// </summary>
		public string? Title { get; set; }
		/// <summary>
		/// プレースフォルダの追加(オプション)
		/// </summary>
		/// <param name="value"></param>
		public void AddPlace( string folder, FDAP fdap );

		public bool? ShowDialog();
		// public bool? ShowDialog( Window ownerWindow );
	}
}
