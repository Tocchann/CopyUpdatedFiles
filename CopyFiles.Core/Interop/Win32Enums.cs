using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Core.Interop;

internal static class Literals
{
	public const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // == "MZ"
	public const uint IMAGE_NT_SIGNATURE = 0x00004550; // == "PE\0\0"
	public const int IMAGE_NUMBEROF_DIRECTORY_ENTRIES = 16;    // IMAGE_DATA_DIRECTORY のエントリー数(固定長配列)
}

internal enum IMAGE_FILE_MACHINE : ushort
{
	UNKNOWN = 0,
	I386 = 0x014C,
	R3000BE = 0x0160,
	R3000LE = 0x0162,
	R4000 = 0x0166,
	R10000 = 0x0168,
	WCEMIPSV2 = 0x0169,
	ALPHA = 0x0184,
	SH3 = 0x01A2,
	SH3DSP = 0x01A3,
	SH3E = 0x01A4,
	SH4 = 0x01A6,
	SH5 = 0x01A8,
	ARM = 0x01C0,
	THUMB = 0x01C2,
	ARM2 = 0x01C4,
	AM33 = 0x01D3,
	POWERPC = 0x01F0,
	POWERPCFP = 0x01F1,
	IA64 = 0x0200,
	MIPS16 = 0x0266,
	ALPHA64 = 0x0284,
	MIPSFPU = 0x0366,
	MIPSFPU16 = 0x0466,
	AXP64 = ALPHA64,
	TRICORE = 0x0520,
	CEF = 0x0CEF,
	EBC = 0x0EBC,
	AMD64 = 0x8664,
	M32R = 0x9041,
	ARM64 = 0xAA64,
	CEE = 0xC0EE
}
[Flags]
internal enum IMAGE_FILE_CHARACTERISTICS : ushort
{
	RELOCS_STRIPPED = 0x0001,  // Relocation info stripped from file.
	EXECUTABLE_IMAGE = 0x0002,  // File is executable  (i.e. no unresolved external references).
	LINE_NUMS_STRIPPED = 0x0004,  // Line nunbers stripped from file.
	LOCAL_SYMS_STRIPPED = 0x0008,  // Local symbols stripped from file.
	AGGRESIVE_WS_TRIM = 0x0010,  // Aggressively trim working set
	LARGE_ADDRESS_AWARE = 0x0020,  // App can handle >2gb addresses
	BYTES_REVERSED_LO = 0x0080,  // Bytes of machine word are reversed.
	_32BIT_MACHINE = 0x0100,  // 32 bit word machine.
	DEBUG_STRIPPED = 0x0200,  // Debugging info stripped from file in .DBG file
	REMOVABLE_RUN_FROM_SWAP = 0x0400,  // If Image is on removable media, copy and run from the swap file.
	NET_RUN_FROM_SWAP = 0x0800,  // If Image is on Net, copy and run from the swap file.
	SYSTEM = 0x1000,  // System File.
	DLL = 0x2000,  // File is a DLL.
	UP_SYSTEM_ONLY = 0x4000,  // File should only be run on a UP machine
	BYTES_REVERSED_HI = 0x8000,  // Bytes of machine word are reversed.
}

internal enum HeaderMagic : ushort
{
	PE32 = 0x010b,
	PE64 = 0x020b,
	ROM = 0x0107,
}
[Flags]
internal enum SectionFlags : uint
{
	TYPE_NO_PAD = 0x00000008,
	CNT_CODE = 0x00000020,
	CNT_INITIALIZED_DATA = 0x00000040,
	CNT_UNINITIALIZED_DATA = 0x00000080,
	LNK_INFO = 0x00000200,
	LNK_REMOVE = 0x00000800,
	LNK_COMDAT = 0x00001000,
	NO_DEFER_SPEC_EXC = 0x00004000,
	GPREL = 0x00008000,
	MEM_FARDATA = 0x00008000,
	MEM_PURGEABLE = 0x00020000,
	MEM_16BIT = 0x00020000,
	MEM_LOCKED = 0x00040000,
	MEM_PRELOAD = 0x00080000,
	ALIGN_1BYTES = 0x00100000,
	ALIGN_2BYTES = 0x00200000,
	ALIGN_4BYTES = 0x00300000,
	ALIGN_8BYTES = 0x00400000,
	ALIGN_16BYTES = 0x00500000,
	ALIGN_32BYTES = 0x00600000,
	ALIGN_64BYTES = 0x00700000,
	ALIGN_128BYTES = 0x00800000,
	ALIGN_256BYTES = 0x00900000,
	ALIGN_512BYTES = 0x00A00000,
	ALIGN_1024BYTES = 0x00B00000,
	ALIGN_2048BYTES = 0x00C00000,
	ALIGN_4096BYTES = 0x00D00000,
	ALIGN_8192BYTES = 0x00E00000,
	ALIGN_MASK = 0x00F00000,
	LNK_NRELOC_OVFL = 0x01000000,
	MEM_DISCARDABLE = 0x02000000,
	MEM_NOT_CACHED = 0x04000000,
	MEM_NOT_PAGED = 0x08000000,
	MEM_SHARED = 0x10000000,
	MEM_EXECUTE = 0x20000000,
	MEM_READ = 0x40000000,
	MEM_WRITE = 0x80000000
}
internal enum IMAGE_DIRECTORY_ENTRY : int
{
	EXPORT, // Export Directory
	IMPORT, // Import Directory
	RESOURCE, // Resource Directory
	EXCEPTION, // Exception Directory
	SECURITY, // Security Directory
	BASERELOC, // Base Relocation Table
	DEBUG, // Debug Directory
	ARCHITECTURE, // Architecture Specific Data
	GLOBALPTR, // RVA of GP
	TLS, // TLS Directory
	LOAD_CONFIG, // Load Configuration Directory
	BOUND_IMPORT, // Bound Import Directory in headers
	IAT, // Import Address Table
	DELAY_IMPORT, // Delay Load Import Descriptors
	COM_DESCRIPTOR, // COM Runtime descriptor
	Reseved, // 15は未定義(要素数が16個あるのでダミーで定義)
}

