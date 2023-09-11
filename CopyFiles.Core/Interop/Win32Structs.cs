using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Core.Interop;

[StructLayout(LayoutKind.Sequential, Pack =1)]
public struct IMAGE_DOS_HEADER
{
	public ushort e_magic;                     // Magic number
	public ushort e_cblp;                      // bytes on last page of file
	public ushort e_cp;                        // Pages in file
	public ushort e_crlc;                      // Relocations
	public ushort e_cparhdr;                   // Size of header in paragraphs
	public ushort e_minalloc;                  // Minimum extra paragraphs needed
	public ushort e_maxalloc;                  // Maximum extra paragraphs needed
	public ushort e_ss;                        // Initial (relative) SS value
	public ushort e_sp;                        // Initial SP value
	public ushort e_csum;                      // Checksum
	public ushort e_ip;                        // Initial IP value
	public ushort e_cs;                        // Initial (relative) CS value
	public ushort e_lfarlc;                    // File address of relocation table
	public ushort e_ovno;                      // Overlay number
	public ushort e_res_0;                    // Reserved ushorts
	public ushort e_res_1;                    // Reserved ushorts
	public ushort e_res_2;                    // Reserved ushorts
	public ushort e_res_3;                    // Reserved ushorts
	public ushort e_oemid;                     // OEM identifier (for e_oeminfo)
	public ushort e_oeminfo;                   // OEM information; e_oemid specific
	public ushort e_res2_0;                  // Reserved words
	public ushort e_res2_1;                  // Reserved words
	public ushort e_res2_2;                  // Reserved words
	public ushort e_res2_3;                  // Reserved words
	public ushort e_res2_4;                  // Reserved words
	public ushort e_res2_5;                  // Reserved words
	public ushort e_res2_6;                  // Reserved words
	public ushort e_res2_7;                  // Reserved words
	public ushort e_res2_8;                  // Reserved words
	public ushort e_res2_9;                  // Reserved words
	public int e_lfanew;                    // File address of new exe header
}

[StructLayout( LayoutKind.Sequential )]
public struct IMAGE_FILE_HEADER
{
	public IMAGE_FILE_MACHINE Machine;
	public ushort NumberOfSections;
	public uint TimeDateStamp;
	public uint PointerToSymbolTable;
	public uint NumberOfSymbols;
	public ushort SizeOfOptionalHeader;
	public IMAGE_FILE_CHARACTERISTICS Characteristics;
}

[StructLayout( LayoutKind.Sequential )]
public struct IMAGE_OPTIONAL_HEADER32
{
	public HeaderMagic Magic;
	public byte MajorLinkerVersion;
	public byte MinorLinkerVersion;
	public uint SizeOfCode;
	public uint SizeOfInitializedData;
	public uint SizeOfUninitializedData;
	public uint AddressOfEntryPoint;
	public uint BaseOfCode;
	public uint BaseOfData;
	public uint ImageBase;
	public uint SectionAlignment;
	public uint FileAlignment;
	public ushort MajorOperatingSystemVersion;
	public ushort MinorOperatingSystemVersion;
	public ushort MajorImageVersion;
	public ushort MinorImageVersion;
	public ushort MajorSubsystemVersion;
	public ushort MinorSubsystemVersion;
	public uint Win32VersionValue;
	public uint SizeOfImage;
	public uint SizeOfHeaders;
	public uint CheckSum;
	public ushort Subsystem;
	public ushort DllCharacteristics;
	public uint SizeOfStackReserve;
	public uint SizeOfStackCommit;
	public uint SizeOfHeapReserve;
	public uint SizeOfHeapCommit;
	public uint LoaderFlags;
	public uint NumberOfRvaAndSizes;
	//IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
}
[StructLayout( LayoutKind.Sequential )]
public struct IMAGE_OPTIONAL_HEADER64
{
	public HeaderMagic Magic;
	public byte MajorLinkerVersion;
	public byte MinorLinkerVersion;
	public uint SizeOfCode;
	public uint SizeOfInitializedData;
	public uint SizeOfUninitializedData;
	public uint AddressOfEntryPoint;
	public uint BaseOfCode;
	public ulong ImageBase;
	public uint SectionAlignment;
	public uint FileAlignment;
	public ushort MajorOperatingSystemVersion;
	public ushort MinorOperatingSystemVersion;
	public ushort MajorImageVersion;
	public ushort MinorImageVersion;
	public ushort MajorSubsystemVersion;
	public ushort MinorSubsystemVersion;
	public uint Win32VersionValue;
	public uint SizeOfImage;
	public uint SizeOfHeaders;
	public uint CheckSum;
	public ushort Subsystem;
	public ushort DllCharacteristics;
	public ulong SizeOfStackReserve;
	public ulong SizeOfStackCommit;
	public ulong SizeOfHeapReserve;
	public ulong SizeOfHeapCommit;
	public uint LoaderFlags;
	public uint NumberOfRvaAndSizes;
	//public IMAGE_DATA_DIRECTORY DataDirectory[Literals.IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
}

[StructLayout( LayoutKind.Sequential )]
public struct IMAGE_DATA_DIRECTORY
{
	public uint VirtualAddress;
	public uint Size;
}

[StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi)]
public struct IMAGE_SECTION_HEADER
{
	[MarshalAs( UnmanagedType.ByValTStr, SizeConst = 8 )]
	public string Name;
	public uint VirtualSize;
	public uint VirtualAddress;
	public uint SizeOfRawData;
	public uint PointerToRawData;
	public uint PointerToRelocations;
	public uint PointerToLinenumbers;
	public ushort NumberOfRelocations;
	public ushort NumberOfLinenumbers;
	public SectionFlags Characteristics;
}
