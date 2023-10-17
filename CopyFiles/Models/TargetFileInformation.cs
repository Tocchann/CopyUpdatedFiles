using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Models;

public enum TargetStatus
{
	/// <summary>
	/// 不明
	/// </summary>
	Unknown,
	/// <summary>
	/// コピー先無し
	/// </summary>
	NotExist,
	/// <summary>
	/// コピー先と内容が異なる
	/// </summary>
	Different,
	/// <summary>
	/// コピー先と内容は異なるがバージョンは同じ
	/// </summary>
	DifferentSameVer,
	/// <summary>
	/// コピー先と完全一致(内容、日付、バージョン)
	/// </summary>
	SameFullMatch,
	/// <summary>
	/// コピー先と一致(日付は違うが内容は同じ)
	/// </summary>
	SameWithoutDate,
	/// <summary>
	/// コピー先とサイズは違うが内容は同じ(署名の有無)
	/// </summary>
	SameWithoutSize,
	/// <summary>
	/// コピー元が未署名(未署名コピー用フラグ)
	/// </summary>
	NotSigned,
}
public partial class TargetFileInformation : TargetInformation
{
	[ObservableProperty]
	TargetStatus status;

	[ObservableProperty]
	bool ignore;

	[ObservableProperty]
	Version? sourceVersion;

	[ObservableProperty]
	Version? destinationVersion;

	public int SourceOffsetPos { get; set; }
	public int DestinationOffsetPos { get; set; }

	public bool NeedCopy
	{
		get
		{
			return Status switch
			{
				TargetStatus.NotExist => true, // コピー先がない
				TargetStatus.Different => true, // コピー先と異なる
				TargetStatus.DifferentSameVer => true, // コピー先と異なるがバージョンが同じ
				TargetStatus.SameFullMatch => false, // コピー先と一致(日付も一致)
				TargetStatus.SameWithoutDate => false, // コピー先と一致(日付が違うだけだったらコピーしなくても問題ないのでコピーしない)
				TargetStatus.SameWithoutSize => false, // サイズは違うが内容は一致(実行ファイルで署名の有無が該当。署名の有無だけなら、変更しない)
				TargetStatus.NotSigned => true, // コピー元に署名がない(未署名処理用)
				_ => throw new ArgumentOutOfRangeException( nameof( Status ) ),
			};
		}
	}
}
