using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Models;

public enum TargetStatus
{
	Unknown, // 不明
	NotExist, // コピー先がない
	Different, // コピー先と異なる
	DifferentSameVer, // コピー先と異なるがバージョンが同じ
	SameFullMatch, // コピー先と一致(日付も一致)
	SameWithoutDate, // コピー先と一致(ただし日付違い。ビルドされたけど内容が変わっていない)
	SameWithoutSize, // サイズは違うが内容は一致(実行ファイルで署名の有無が該当)
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
													   //TargetStatus.Unknown => false, // 不明はプログラム的にいただけない状態
				_ => throw new ArgumentOutOfRangeException( nameof( Status ) ),
			};
		}
	}
}
