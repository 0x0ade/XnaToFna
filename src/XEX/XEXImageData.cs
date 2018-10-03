// Port of xenonImageLoader.cpp in rexdex's "recompiler" project.
// https://github.com/rexdex/recompiler/blob/a81b4faae28413129a9720bda1904ebaf0521021/dev/src/xenon_decompiler/xenonImageLoader.cpp
// The original file doesn't contain a copy of the MIT license, so I'm just dropping it here. -ade
/*
MIT License

Copyright (c) 2017 Tomasz Jonarski (RexDex)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using MonoMod;
using MonoMod.InlineRT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XnaToFna.ContentTransformers;
using static XnaToFna.ContentHelper;
using static XnaToFna.XEX.XEXImageData.XEXHeaderKeys;
using static XnaToFna.XEX.XEXImageData.XEXModuleFlags;
using static XnaToFna.XEX.XEXImageData.XEXSystemFlags;
using static XnaToFna.XEX.XEXImageData.XEXAprovalType;
using static XnaToFna.XEX.XEXImageData.XEXEncryptionType;
using static XnaToFna.XEX.XEXImageData.XEXCompressionType;
using static XnaToFna.XEX.XEXImageData.XEXImageFlags;
using static XnaToFna.XEX.XEXImageData.XEXMediaFlags;
using static XnaToFna.XEX.XEXImageData.XEXRegion;
using static XnaToFna.XEX.XEXImageData.XEXSectionType;
using System.Security.Cryptography;

namespace XnaToFna.XEX {
    public class XEXImageData {

        public const uint XEX2_SECTION_LENGTH = 0x00010000;

        readonly static byte[] IV = new byte[16]; // No IV.

        // Key for retail executables
        readonly static byte[] xe_xex2_retail_key =
        {
            0x20, 0xB1, 0x85, 0xA5, 0x9D, 0x28, 0xFD, 0xC3,
            0x40, 0x58, 0x3F, 0xBB, 0x08, 0x96, 0xBF, 0x91
        };

        // Key for devkit executables
        readonly static byte[] xe_xex2_devkit_key =
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public class XEXRatings {
            public byte rating_esrb;
            public byte rating_pegi;
            public byte rating_pegifi;
            public byte rating_pegipt;
            public byte rating_bbfc;
            public byte rating_cero;
            public byte rating_usk;
            public byte rating_oflcau;
            public byte rating_oflcnz;
            public byte rating_kmrb;
            public byte rating_brazil;
            public byte rating_fpb;
            public XEXRatings(BinaryReader reader) {
                rating_esrb = reader.ReadByte();
                rating_pegi = reader.ReadByte();
                rating_pegifi = reader.ReadByte();
                rating_pegipt = reader.ReadByte();
                rating_bbfc = reader.ReadByte();
                rating_cero = reader.ReadByte();
                rating_usk = reader.ReadByte();
                rating_oflcau = reader.ReadByte();
                rating_oflcnz = reader.ReadByte();
                rating_kmrb = reader.ReadByte();
                rating_brazil = reader.ReadByte();
                rating_fpb = reader.ReadByte();
            }
        }

        /*
        union XEXVersion
        {
          uint32	value;

          struct
          {
            uint32	major   : 4;
            uint32	minor   : 4;
            uint32	build   : 16;
            uint32	qfe     : 8;
          };
        };
        */
        public class XEXVersion {
            public uint value;
            public XEXVersion(BinaryReader reader) {
                value = SwapEndian(true, reader.ReadUInt32());
            }
        }

        public class XEXOptionalHeader {
            public uint offset;
            public uint length;
            public uint value;
            public XEXHeaderKeys key;
            public XEXOptionalHeader(BinaryReader reader) {
                key = (XEXHeaderKeys) SwapEndian(true, reader.ReadUInt32());
                offset = SwapEndian(true, reader.ReadUInt32());
                length = 0;
                value = 0;
            }
        }

        public class XEXResourceInfo {
            public string name;
            public uint address;
            public uint size;
            public XEXResourceInfo(BinaryReader reader) {
                name = new string(reader.ReadChars(8)).TrimEnd('\0');
                address = SwapEndian(true, reader.ReadUInt32());
                size = SwapEndian(true, reader.ReadUInt32());
            }
        }

        public class XEXExecutionInfo {
            public uint media_id;
            public XEXVersion version;
            public XEXVersion base_version;
            public uint title_id;
            public byte platform;
            public byte executable_table;
            public byte disc_number;
            public byte disc_count;
            public uint savegame_id;
            public XEXExecutionInfo(BinaryReader reader) {
                media_id = SwapEndian(true, reader.ReadUInt32());
                version = new XEXVersion(reader);
                base_version = new XEXVersion(reader);
                title_id = SwapEndian(true, reader.ReadUInt32());
                platform = reader.ReadByte();
                executable_table = reader.ReadByte();
                disc_number = reader.ReadByte();
                disc_count = reader.ReadByte();
                savegame_id = SwapEndian(true, reader.ReadUInt32());
            }
        }

        public class XEXTLSInfo {
            public uint slot_count;
            public uint raw_data_address;
            public uint data_size;
            public uint raw_data_size;
            public XEXTLSInfo(BinaryReader reader) {
                slot_count = SwapEndian(true, reader.ReadUInt32());
                raw_data_address = SwapEndian(true, reader.ReadUInt32());
                data_size = SwapEndian(true, reader.ReadUInt32());
                raw_data_size = SwapEndian(true, reader.ReadUInt32());
            }
        }

        public class XEXImportLibraryBlockHeader {
            public uint string_table_size;
            public uint count;
            public XEXImportLibraryBlockHeader(BinaryReader reader) {
                string_table_size = SwapEndian(true, reader.ReadUInt32());
                count = SwapEndian(true, reader.ReadUInt32());
            }
        }

        public class XEXImportLibraryHeader {
            public uint unknown;
            public byte[] digest = new byte[20];
            public uint import_id;
            public XEXVersion version;
            public XEXVersion min_version;
            public ushort name_index;
            public ushort record_count;
            public XEXImportLibraryHeader(BinaryReader reader) {
                unknown = SwapEndian(true, reader.ReadUInt32());
                reader.BaseStream.Read(digest, 0, digest.Length);
                version = new XEXVersion(reader);
                min_version = new XEXVersion(reader);
                name_index = SwapEndian(true, reader.ReadUInt16());
                record_count = SwapEndian(true, reader.ReadUInt16());
            }
        }

        public class XEXStaticLibrary {
            public string name;
            public ushort major;
            public ushort minor;
            public ushort build;
            public ushort qfe;
            public XEXAprovalType approval;
            public XEXStaticLibrary(BinaryReader reader) {
                name = new string(reader.ReadChars(8)).TrimEnd('\0');
                major = SwapEndian(true, reader.ReadUInt16());
                minor = SwapEndian(true, reader.ReadUInt16());
                build = SwapEndian(true, reader.ReadUInt16());
                qfe = SwapEndian(true, reader.ReadUInt16());
                approval = (XEXAprovalType) SwapEndian(true, reader.ReadUInt32());
            }
        }

        public class XEXFileBasicCompressionBlock {
            public uint data_size;
            public uint zero_size;
            public XEXFileBasicCompressionBlock(BinaryReader reader) {
                data_size = SwapEndian(true, reader.ReadUInt32());
                zero_size = SwapEndian(true, reader.ReadUInt32());
            }
        }

        public class XEXFileNormalCompressionInfo {
            public uint window_size;
            public uint window_bits;
            public uint block_size;
            public byte[] block_hash = new byte[20];
            public XEXFileNormalCompressionInfo(BinaryReader reader) {
                window_size = SwapEndian(true, reader.ReadUInt32());
                // window_bits = reader.ReadUInt32();
                block_size = SwapEndian(true, reader.ReadUInt32());
                reader.BaseStream.Read(block_hash, 0, block_hash.Length);
                uint temp = window_size;
                for (uint m = 0; m < 32; m++, window_bits++) {
                    temp <<= 1;
                    if (temp == 0x80000000) {
                        break;
                    }
                }
            }
        }

        public class XEXEncryptionHeader {
            public XEXEncryptionType encryption_type;
            public XEXCompressionType compression_type;
            public XEXEncryptionHeader(BinaryReader reader) {
                encryption_type = (XEXEncryptionType) SwapEndian(true, reader.ReadUInt16());
                compression_type = (XEXCompressionType) SwapEndian(true, reader.ReadUInt16());
            }
        }

        public class XEXFileFormat {
            public XEXEncryptionType encryption_type;
            public XEXCompressionType compression_type;

            // basic compression blocks (in case of basic compression)
            public List<XEXFileBasicCompressionBlock> basic_blocks = new List<XEXFileBasicCompressionBlock>();

            // normal compression
            public XEXFileNormalCompressionInfo normal;
        }

        public class XEXLoaderInfo {
            public uint header_size;
            public uint image_size;
            public byte[] rsa_signature = new byte[256];
            public uint unklength;
            public uint image_flags;
            public uint load_address;
            public byte[] section_digest = new byte[20];
            public uint import_table_count;
            public byte[] import_table_digest = new byte[20];
            public byte[] media_id = new byte[16];
            public byte[] file_key = new byte[16];
            public uint export_table;
            public byte[] header_digest = new byte[20];
            public uint game_regions;
            public uint media_flags;
            public XEXLoaderInfo(BinaryReader reader) {
                header_size = SwapEndian(true, reader.ReadUInt32());
                image_size = SwapEndian(true, reader.ReadUInt32());
                reader.BaseStream.Read(rsa_signature, 0, rsa_signature.Length);
                unklength = SwapEndian(true, reader.ReadUInt32());
                image_flags = SwapEndian(true, reader.ReadUInt32());
                load_address = SwapEndian(true, reader.ReadUInt32());
                reader.BaseStream.Read(section_digest, 0, section_digest.Length);
                import_table_count = SwapEndian(true, reader.ReadUInt32());
                reader.BaseStream.Read(import_table_digest, 0, import_table_digest.Length);
                reader.BaseStream.Read(media_id, 0, media_id.Length);
                reader.BaseStream.Read(file_key, 0, file_key.Length);
                export_table = SwapEndian(true, reader.ReadUInt32());
                reader.BaseStream.Read(header_digest, 0, header_digest.Length);
                game_regions = SwapEndian(true, reader.ReadUInt32());
                media_flags = SwapEndian(true, reader.ReadUInt32());
            }
        }

        /*
        struct XEXSection
        {
            union
            {
                struct
                {
                    uint32	type        : 4; // XEXSectionType
                    uint32	page_count	: 28;   // # of 64kb pages
                };

                uint32	value;    // To make uint8_t swapping easier
            } info;

            uint8 digest[20];
        };
        */
        public class XEXSection {
            public uint info_value;
            public byte[] digest = new byte[20];
            public XEXSection(BinaryReader reader) {
                info_value = SwapEndian(true, reader.ReadUInt32());
                reader.BaseStream.Read(digest, 0, digest.Length);
            }
        }

        public class XEXHeader {
            public uint xex2;
            public uint module_flags;
            public uint exe_offset;
            public uint unknown0;
            public uint certificate_offset;
            public uint header_count;
            public XEXHeader(BinaryReader reader) {
                xex2 = SwapEndian(true, reader.ReadUInt32());
                module_flags = SwapEndian(true, reader.ReadUInt32());
                exe_offset = SwapEndian(true, reader.ReadUInt32());
                unknown0 = SwapEndian(true, reader.ReadUInt32());
                certificate_offset = SwapEndian(true, reader.ReadUInt32());
                header_count = SwapEndian(true, reader.ReadUInt32());
            }
        }

        public enum XEXHeaderKeys : uint {
            XEX_HEADER_RESOURCE_INFO = 0x000002FF,
            XEX_HEADER_FILE_FORMAT_INFO = 0x000003FF,
            XEX_HEADER_DELTA_PATCH_DESCRIPTOR = 0x000005FF,
            XEX_HEADER_BASE_REFERENCE = 0x00000405,
            XEX_HEADER_BOUNDING_PATH = 0x000080FF,
            XEX_HEADER_DEVICE_ID = 0x00008105,
            XEX_HEADER_ORIGINAL_BASE_ADDRESS = 0x00010001,
            XEX_HEADER_ENTRY_POINT = 0x00010100,
            XEX_HEADER_IMAGE_BASE_ADDRESS = 0x00010201,
            XEX_HEADER_IMPORT_LIBRARIES = 0x000103FF,
            XEX_HEADER_CHECKSUM_TIMESTAMP = 0x00018002,
            XEX_HEADER_ENABLED_FOR_CALLCAP = 0x00018102,
            XEX_HEADER_ENABLED_FOR_FASTCAP = 0x00018200,
            XEX_HEADER_ORIGINAL_PE_NAME = 0x000183FF,
            XEX_HEADER_STATIC_LIBRARIES = 0x000200FF,
            XEX_HEADER_TLS_INFO = 0x00020104,
            XEX_HEADER_DEFAULT_STACK_SIZE = 0x00020200,
            XEX_HEADER_DEFAULT_FILESYSTEM_CACHE_SIZE = 0x00020301,
            XEX_HEADER_DEFAULT_HEAP_SIZE = 0x00020401,
            XEX_HEADER_PAGE_HEAP_SIZE_AND_FLAGS = 0x00028002,
            XEX_HEADER_SYSTEM_FLAGS = 0x00030000,
            XEX_HEADER_EXECUTION_INFO = 0x00040006,
            XEX_HEADER_TITLE_WORKSPACE_SIZE = 0x00040201,
            XEX_HEADER_GAME_RATINGS = 0x00040310,
            XEX_HEADER_LAN_KEY = 0x00040404,
            XEX_HEADER_XBOX360_LOGO = 0x000405FF,
            XEX_HEADER_MULTIDISC_MEDIA_IDS = 0x000406FF,
            XEX_HEADER_ALTERNATE_TITLE_IDS = 0x000407FF,
            XEX_HEADER_ADDITIONAL_TITLE_MEMORY = 0x00040801,
            XEX_HEADER_EXPORTS_BY_NAME = 0x00E10402,
        }

        [Flags]
        public enum XEXModuleFlags : ushort {
            XEX_MODULE_TITLE = 0x00000001,
            XEX_MODULE_EXPORTS_TO_TITLE = 0x00000002,
            XEX_MODULE_SYSTEM_DEBUGGER = 0x00000004,
            XEX_MODULE_DLL_MODULE = 0x00000008,
            XEX_MODULE_MODULE_PATCH = 0x00000010,
            XEX_MODULE_PATCH_FULL = 0x00000020,
            XEX_MODULE_PATCH_DELTA = 0x00000040,
            XEX_MODULE_USER_MODE = 0x00000080,
        }

        [Flags]
        public enum XEXSystemFlags : uint {
            XEX_SYSTEM_NO_FORCED_REBOOT = 0x00000001,
            XEX_SYSTEM_FOREGROUND_TASKS = 0x00000002,
            XEX_SYSTEM_NO_ODD_MAPPING = 0x00000004,
            XEX_SYSTEM_HANDLE_MCE_INPUT = 0x00000008,
            XEX_SYSTEM_RESTRICTED_HUD_FEATURES = 0x00000010,
            XEX_SYSTEM_HANDLE_GAMEPAD_DISCONNECT = 0x00000020,
            XEX_SYSTEM_INSECURE_SOCKETS = 0x00000040,
            XEX_SYSTEM_XBOX1_INTEROPERABILITY = 0x00000080,
            XEX_SYSTEM_DASH_CONTEXT = 0x00000100,
            XEX_SYSTEM_USES_GAME_VOICE_CHANNEL = 0x00000200,
            XEX_SYSTEM_PAL50_INCOMPATIBLE = 0x00000400,
            XEX_SYSTEM_INSECURE_UTILITY_DRIVE = 0x00000800,
            XEX_SYSTEM_XAM_HOOKS = 0x00001000,
            XEX_SYSTEM_ACCESS_PII = 0x00002000,
            XEX_SYSTEM_CROSS_PLATFORM_SYSTEM_LINK = 0x00004000,
            XEX_SYSTEM_MULTIDISC_SWAP = 0x00008000,
            XEX_SYSTEM_MULTIDISC_INSECURE_MEDIA = 0x00010000,
            XEX_SYSTEM_AP25_MEDIA = 0x00020000,
            XEX_SYSTEM_NO_CONFIRM_EXIT = 0x00040000,
            XEX_SYSTEM_ALLOW_BACKGROUND_DOWNLOAD = 0x00080000,
            XEX_SYSTEM_CREATE_PERSISTABLE_RAMDRIVE = 0x00100000,
            XEX_SYSTEM_INHERIT_PERSISTENT_RAMDRIVE = 0x00200000,
            XEX_SYSTEM_ALLOW_HUD_VIBRATION = 0x00400000,
            XEX_SYSTEM_ACCESS_UTILITY_PARTITIONS = 0x00800000,
            XEX_SYSTEM_IPTV_INPUT_SUPPORTED = 0x01000000,
            XEX_SYSTEM_PREFER_BIG_BUTTON_INPUT = 0x02000000,
            XEX_SYSTEM_ALLOW_EXTENDED_SYSTEM_RESERVATION = 0x04000000,
            XEX_SYSTEM_MULTIDISC_CROSS_TITLE = 0x08000000,
            XEX_SYSTEM_INSTALL_INCOMPATIBLE = 0x10000000,
            XEX_SYSTEM_ALLOW_AVATAR_GET_METADATA_BY_XUID = 0x20000000,
            XEX_SYSTEM_ALLOW_CONTROLLER_SWAPPING = 0x40000000,
            XEX_SYSTEM_DASH_EXTENSIBILITY_MODULE = 0x80000000,
            // TODO: figure out how stored
            /*XEX_SYSTEM_ALLOW_NETWORK_READ_CANCEL            = 0x0,
            XEX_SYSTEM_UNINTERRUPTABLE_READS                = 0x0,
            XEX_SYSTEM_REQUIRE_FULL_EXPERIENCE              = 0x0,
            XEX_SYSTEM_GAME_VOICE_REQUIRED_UI               = 0x0,
            XEX_SYSTEM_CAMERA_ANGLE                         = 0x0,
            XEX_SYSTEM_SKELETAL_TRACKING_REQUIRED           = 0x0,
            XEX_SYSTEM_SKELETAL_TRACKING_SUPPORTED          = 0x0,*/
        }

        public enum XEXAprovalType : ushort {
            XEX_APPROVAL_UNAPPROVED = 0,
            XEX_APPROVAL_POSSIBLE = 1,
            XEX_APPROVAL_APPROVED = 2,
            XEX_APPROVAL_EXPIRED = 3,
        }

        public enum XEXEncryptionType : ushort {
            XEX_ENCRYPTION_NONE = 0,
            XEX_ENCRYPTION_NORMAL = 1,
        }

        public enum XEXCompressionType : ushort {
            XEX_COMPRESSION_NONE = 0,
            XEX_COMPRESSION_BASIC = 1,
            XEX_COMPRESSION_NORMAL = 2,
            XEX_COMPRESSION_DELTA = 3,
        }

        [Flags]
        public enum XEXImageFlags : uint {
            XEX_IMAGE_MANUFACTURING_UTILITY = 0x00000002,
            XEX_IMAGE_MANUFACTURING_SUPPORT_TOOLS = 0x00000004,
            XEX_IMAGE_XGD2_MEDIA_ONLY = 0x00000008,
            XEX_IMAGE_CARDEA_KEY = 0x00000100,
            XEX_IMAGE_XEIKA_KEY = 0x00000200,
            XEX_IMAGE_USERMODE_TITLE = 0x00000400,
            XEX_IMAGE_USERMODE_SYSTEM = 0x00000800,
            XEX_IMAGE_ORANGE0 = 0x00001000,
            XEX_IMAGE_ORANGE1 = 0x00002000,
            XEX_IMAGE_ORANGE2 = 0x00004000,
            XEX_IMAGE_IPTV_SIGNUP_APPLICATION = 0x00010000,
            XEX_IMAGE_IPTV_TITLE_APPLICATION = 0x00020000,
            XEX_IMAGE_KEYVAULT_PRIVILEGES_REQUIRED = 0x04000000,
            XEX_IMAGE_ONLINE_ACTIVATION_REQUIRED = 0x08000000,
            XEX_IMAGE_PAGE_SIZE_4KB = 0x10000000, // else 64KB
            XEX_IMAGE_REGION_FREE = 0x20000000,
            XEX_IMAGE_REVOCATION_CHECK_OPTIONAL = 0x40000000,
            XEX_IMAGE_REVOCATION_CHECK_REQUIRED = 0x80000000,
        }

        [Flags]
        public enum XEXMediaFlags : uint {
            XEX_MEDIA_HARDDISK = 0x00000001,
            XEX_MEDIA_DVD_X2 = 0x00000002,
            XEX_MEDIA_DVD_CD = 0x00000004,
            XEX_MEDIA_DVD_5 = 0x00000008,
            XEX_MEDIA_DVD_9 = 0x00000010,
            XEX_MEDIA_SYSTEM_FLASH = 0x00000020,
            XEX_MEDIA_MEMORY_UNIT = 0x00000080,
            XEX_MEDIA_USB_MASS_STORAGE_DEVICE = 0x00000100,
            XEX_MEDIA_NETWORK = 0x00000200,
            XEX_MEDIA_DIRECT_FROM_MEMORY = 0x00000400,
            XEX_MEDIA_RAM_DRIVE = 0x00000800,
            XEX_MEDIA_SVOD = 0x00001000,
            XEX_MEDIA_INSECURE_PACKAGE = 0x01000000,
            XEX_MEDIA_SAVEGAME_PACKAGE = 0x02000000,
            XEX_MEDIA_LOCALLY_SIGNED_PACKAGE = 0x04000000,
            XEX_MEDIA_LIVE_SIGNED_PACKAGE = 0x08000000,
            XEX_MEDIA_XBOX_PACKAGE = 0x10000000,
        }

        public enum XEXRegion : uint {
            XEX_REGION_NTSCU = 0x000000FF,
            XEX_REGION_NTSCJ = 0x0000FF00,
            XEX_REGION_NTSCJ_JAPAN = 0x00000100,
            XEX_REGION_NTSCJ_CHINA = 0x00000200,
            XEX_REGION_PAL = 0x00FF0000,
            XEX_REGION_PAL_AU_NZ = 0x00010000,
            XEX_REGION_OTHER = 0xFF000000,
            XEX_REGION_ALL = 0xFFFFFFFF,
        }

        public enum XEXSectionType : ushort {
            XEX_SECTION_CODE = 1,
            XEX_SECTION_DATA = 2,
            XEX_SECTION_READONLY_DATA = 3,
        }

        // header stuff
        public XEXHeader header;
        public XEXSystemFlags system_flags;
        public XEXExecutionInfo execution_info;
        public XEXRatings game_ratings;
        public XEXTLSInfo tls_info;
        public XEXFileFormat file_format_info = new XEXFileFormat();
        public XEXLoaderInfo loader_info;
        public byte[] session_key = new byte[16];

        // executable info
        public uint exe_address;
        public uint exe_entry_point;
        public uint exe_stack_size;
        public uint exe_heap_size;

        // embedded resources
        public List<XEXResourceInfo> resources = new List<XEXResourceInfo>();

        // file optional headers
        public List<XEXOptionalHeader> optional_headers = new List<XEXOptionalHeader>();

        // data sections
        public List<XEXSection> sections = new List<XEXSection>();

        // import records
        public List<uint> import_records = new List<uint>();

        // super class fields that we need
        public byte[] m_memoryData;
        public int m_memorySize;

        public XEXImageData(BinaryReader reader) {
            LoadHeaders(reader);
            LoadImageData(reader);
            // LoadPEImage(reader);
        }

        private void LoadHeaders(BinaryReader reader) {
            header = new XEXHeader(reader);
            if (header.xex2 != 0x58455832) {
                throw new InvalidDataException($"File not a XEX2 file - magic numbers in file: 0x{header.xex2.ToString("X8")}");
            }

            long prevPos;

            // process local headers
            for (uint n = 0; n < header.header_count; n++) {
                // load the header
                XEXOptionalHeader optionalHeader = new XEXOptionalHeader(reader);

                // extract the length
                bool add = true;
                switch ((uint) optionalHeader.key & 0xFF) {
                    // just the data
                    case 0x00:
                    case 0x01: {
                            optionalHeader.value = optionalHeader.offset;
                            optionalHeader.offset = 0;
                            break;
                        }

                    // data
                    case 0xFF: {
                            prevPos = reader.BaseStream.Position;
                            reader.BaseStream.Seek(optionalHeader.offset, SeekOrigin.Begin);
                            optionalHeader.length = SwapEndian(true, reader.ReadUInt32());
                            reader.BaseStream.Seek(prevPos, SeekOrigin.Begin);
                            optionalHeader.offset += 4;

                            // too big ?
                            if (optionalHeader.length + optionalHeader.offset >= reader.BaseStream.Length) {
                                throw new InvalidDataException($"Optional header {n} (0x{optionalHeader.key.ToString("X8")}) crosses file boundary. Will not be read.");
                            }

                            break;
                        }

                    // small data
                    default: {
                            optionalHeader.length = ((uint) optionalHeader.key & 0xFF) * 4;

                            // too big ?
                            if (optionalHeader.length + optionalHeader.offset >= reader.BaseStream.Length) {
                                throw new InvalidDataException($"Optional header {n} (0x{optionalHeader.key.ToString("X8")}) crosses file boundary. Will not be read.");
                            }

                            break;
                        }
                }

                // store local optional header
                if (add) {
                    optional_headers.Add(optionalHeader);
                }
            }

            // process the optional headers
            for (int opti = 0; opti < optional_headers.Count; ++opti) {
                XEXOptionalHeader opt = optional_headers[opti];

                // go to the header offset
                if (opt.length > 0 && opt.offset != 0) {
                    reader.BaseStream.Seek(opt.offset, SeekOrigin.Begin);
                }

                // process the optional headers
                switch (opt.key) {
                    // System flags
                    case XEX_HEADER_SYSTEM_FLAGS: {
                            system_flags = (XEXSystemFlags) opt.value;
                            break;
                        }

                    // Resource info repository
                    case XEX_HEADER_RESOURCE_INFO: {
                            // get the count
                            uint count = (opt.length - 4) / 16;
                            resources.Clear();

                            // move to file position
                            for (uint n = 0; n < count; ++n) {
                                // load the resource entry
                                resources.Add(new XEXResourceInfo(reader));
                            }

                            break;
                        }

                    // Execution info
                    case XEX_HEADER_EXECUTION_INFO: {
                            execution_info = new XEXExecutionInfo(reader);
                            break;
                        }

                    // Game ratings
                    case XEX_HEADER_GAME_RATINGS: {
                            break;
                        }

                    // TLS info
                    case XEX_HEADER_TLS_INFO: {
                            tls_info = new XEXTLSInfo(reader);
                            break;
                        }

                    // Base address
                    case XEX_HEADER_IMAGE_BASE_ADDRESS: {
                            exe_address = opt.value;
                            Console.WriteLine("XEX: Found base addrses: 0x{0}", exe_address.ToString("X8"));
                            break;
                        }

                    // Entry point
                    case XEX_HEADER_ENTRY_POINT: {
                            exe_entry_point = opt.value;
                            Console.WriteLine("XEX: Found entry point: 0x{0}", exe_entry_point.ToString("X8"));
                            break;
                        }

                    // Default stack size
                    case XEX_HEADER_DEFAULT_STACK_SIZE: {
                            exe_stack_size = opt.value;
                            break;
                        }

                    // Default heap size
                    case XEX_HEADER_DEFAULT_HEAP_SIZE: {
                            exe_heap_size = opt.value;
                            break;
                        }

                    // File format information
                    case XEX_HEADER_FILE_FORMAT_INFO: {
                            // load the encryption type
                            XEXEncryptionHeader encHeader = new XEXEncryptionHeader(reader);

                            // setup header info
                            file_format_info.encryption_type = (XEXEncryptionType) encHeader.encryption_type;
                            file_format_info.compression_type = (XEXCompressionType) encHeader.compression_type;

                            // load compression blocks
                            switch (encHeader.compression_type) {
                                case XEX_COMPRESSION_NONE: {
                                        Console.WriteLine("XEX: image::Binary is using no compression");
                                        break;
                                    }

                                case XEX_COMPRESSION_DELTA: {
                                        Console.WriteLine("XEX: image::Binary is using unsupported delta compression");
                                        break;
                                    }

                                case XEX_COMPRESSION_BASIC: {
                                        // get the block count
                                        uint block_count = (opt.length - 8) / 8;
                                        file_format_info.basic_blocks.Clear();

                                        // load the basic compression blocks
                                        for (uint bi = 0; bi < block_count; ++bi) {
                                            XEXFileBasicCompressionBlock block = new XEXFileBasicCompressionBlock(reader);
                                            file_format_info.basic_blocks.Add(block);
                                        }

                                        Console.WriteLine("XEX: image::Binary is using basic compression with {0} blocks", block_count);
                                        break;
                                    }

                                case XEX_COMPRESSION_NORMAL: {
                                        file_format_info.normal = new XEXFileNormalCompressionInfo(reader);

                                        Console.WriteLine("XEX: image::Binary is using normal compression with block size = {0}", file_format_info.normal.block_size);
                                        break;
                                    }
                            }

                            // encryption type
                            if (encHeader.encryption_type != XEX_ENCRYPTION_NONE) {
                                Console.WriteLine("XEX: image::Binary is encrypted");
                            }

                            // opt header
                            break;
                        }

                    // Import libraries - We couldn't care less.
                    case XEX_HEADER_IMPORT_LIBRARIES: {
                            // Load the header data
                            XEXImportLibraryBlockHeader blockHeader = new XEXImportLibraryBlockHeader(reader);

                            // get the string data
                            long string_table = reader.BaseStream.Position;
                            reader.BaseStream.Seek(blockHeader.string_table_size, SeekOrigin.Current);

                            // load the imports
                            for (uint m = 0; m < blockHeader.count; m++) {
                                XEXImportLibraryHeader header = new XEXImportLibraryHeader(reader);

                                /*
                                // get the library name
                                string name = "Unknown";
                                short name_index = header.name_index & 0xFF;
                                for (uint i = 0, j = 0; i < blockHeader.string_table_size;) {
                                    Debug.Assert(j <= 0xFF);
                                    if (j == name_index) {
                                        name = string_table + i;
                                        break;
                                    }

                                    if (string_table[i] == 0) {
                                        i++;
                                        if (i % 4 != 0) {
                                            i += 4 - (i % 4);
                                        }
                                        j++;
                                    } else {
                                        i++;
                                    }
                                }

                                // save the import lib name
                                if (name[0] != 0) {
                                    Console.WriteLine("Found import library: '{0}'", name);
                                    m_libNames.push_back(name);
                                }
                                */

                                // load the records
                                for (uint i = 0; i < header.record_count; ++i) {
                                    // load the record entry and add to the global record entry list
                                    import_records.Add(SwapEndian(true, reader.ReadUInt32()));
                                }
                            }

                            // done
                            break;
                        }
                }
            }

            // load the loader info
            {
                // go to the certificate region
                reader.BaseStream.Seek(header.certificate_offset, SeekOrigin.Begin);

                // load the loader info
                loader_info = new XEXLoaderInfo(reader);

                // print some stats
                Console.WriteLine("XEX: Binary size: 0x{0}", loader_info.image_size.ToString("X8"));
            }

            // load the sections
            {
                // go to the section region
                reader.BaseStream.Seek(header.certificate_offset + 0x180, SeekOrigin.Begin);

                // get the section count
                uint sectionCount = SwapEndian(true, reader.ReadUInt32());

                // load the sections
                for (uint i = 0; i < sectionCount; ++i) {
                    sections.Add(new XEXSection(reader));
                }
            }

            // decrypt the XEX key
            {

                // Guess key based on file info.
                byte[] keyToUse = xe_xex2_devkit_key;
                if (execution_info.title_id != 0) {
                    Console.WriteLine("XEX: Found TitleID 0x{0}", execution_info.title_id.ToString("X8"));
                    //if ( m_xexData.system_flags 
                    keyToUse = xe_xex2_retail_key;
                }

                // Decrypt the header and session key
                using (Aes aes = new AesManaged()) {
                    aes.Mode = CipherMode.CBC; // ???
                    aes.BlockSize = 128; // AES block size always 128
                    aes.KeySize = 128;
                    aes.Key = keyToUse;
                    aes.Padding = PaddingMode.None;
                    aes.IV = IV;

                    using (ICryptoTransform decrypt = aes.CreateDecryptor()) {
                        session_key = new byte[16];
                        decrypt.TransformBlock(
                            loader_info.file_key, 0,
                            16,
                            session_key, 0
                        );
                    }

                    /*
                    // stats
                    {
                        const uint32* keys = (const uint32*) &m_xexData.loader_info.file_key;
                        log.Log("XEX: Decrypted file key: %08X-%08X-%08X-%08X", keys[0], keys[1], keys[2], keys[3]);

                        const uint32* skeys = (const uint32*) &m_xexData.session_key;
                        log.Log("XEX: Decrypted session key: %08X-%08X-%08X-%08X", skeys[0], skeys[1], skeys[2], skeys[3]);
                    }
                    */
                }

            }

            // headers loaded
        }

        private void LoadImageDataUncompressed(BinaryReader data) {
            // The EXE image memory is just the XEX memory - exe offset
            int memorySize = (int) (data.BaseStream.Length - header.exe_offset);

            // sanity check
            const uint maxImageSize = 128 << 20;
            if (memorySize >= maxImageSize) {
                throw new InvalidDataException($"Computed image size is to big (0x{memorySize.ToString("X8")}), the exe offset = 0x{header.exe_offset.ToString("X8")}");
            }

            data.BaseStream.Seek(header.exe_offset, SeekOrigin.Begin);
            byte[] sourceData = data.ReadBytes(memorySize);

            if (file_format_info.encryption_type == XEX_ENCRYPTION_NONE) {
                m_memoryData = sourceData;
                m_memorySize = memorySize;
                return;
            }

            // Allocate in-place the XEX memory.
            byte[] memory = new byte[memorySize];

            // Decrypt the image
            DecryptBuffer(session_key, sourceData, memorySize, memory, memorySize);

            // done
            m_memoryData = memory;
            m_memorySize = memorySize;
        }

        private void LoadImageDataNormal(BinaryReader data) {
            // Image source
            int sourceSize = (int) (data.BaseStream.Length - header.exe_offset);
            data.BaseStream.Seek(header.exe_offset, SeekOrigin.Begin);
            byte[] sourceBuffer = data.ReadBytes(sourceSize);

            // Get data
            int imageSize = sourceSize;
            byte[] imageData = sourceBuffer;
            if (file_format_info.encryption_type == XEX_ENCRYPTION_NORMAL) {
                // allocate new buffer and decode it
                imageData = new byte[sourceSize];
                DecryptBuffer(session_key, sourceBuffer, sourceSize, imageData, imageSize);
            }

            // Allocate deblocked data
            byte[] compressData = new byte[imageSize];

            // Compute buffer size
            int compressedSize = 0;
            int uncompressedSize = 0;
            {
                int p = 0; // imageData
                int d = 0; // compressData

                int blockSize = (int) file_format_info.normal.block_size;
                while (blockSize != 0) {
                    int pnext = p + blockSize;
                    int nextSize = (int) SwapEndian(true, BitConverter.ToUInt32(imageData, p));
                    p += 4;
                    p += 20;  // skip 20b hash

                    while (true) {
                        int chunkSize = (imageData[p + 0] << 8) | imageData[p + 1];
                        p += 2;
                        if (chunkSize == 0)
                            break;

                        Array.Copy(imageData, p, compressData, d, chunkSize);
                        p += chunkSize;
                        d += chunkSize;

                        uncompressedSize += 0x8000;
                    }

                    p = pnext;
                    blockSize = nextSize;
                }

                compressedSize = d;
            }

            // Report sizes
            Console.WriteLine("Uncompressed image size: {0}", uncompressedSize);
            Console.WriteLine("Compressed image size: {0}", compressedSize);

            // Allocate in-place the XEX memory
            byte[] uncompressedImage = new byte[uncompressedSize];

            // Setup decompressor and decompress.
            LzxDecoder decoder = new LzxDecoder((int) file_format_info.normal.window_bits);
            int ret;
            using (MemoryStream compressStream = new MemoryStream(compressData))
            using (MemoryStream uncompressedStream = new MemoryStream(uncompressedImage))
                if ((ret = decoder.Decompress(compressStream, compressedSize, uncompressedStream, uncompressedSize)) != 0)
                    // The original source says "Unable to decompression image data", let's just fix that. -ade
                    throw new InvalidDataException($"Unable to decompress image data: {ret}");

            // done
            Console.WriteLine("Image data decompressed");

            // set image data
            m_memoryData = uncompressedImage;
            m_memorySize = uncompressedSize;
        }

        private void LoadImageDataBasic(BinaryReader data) {
            // calculate the uncompressed size
            int memorySize = 0;
            int blockCount = file_format_info.basic_blocks.Count;
            for (int i = 0; i < blockCount; ++i) {
                XEXFileBasicCompressionBlock block = file_format_info.basic_blocks[i];
                memorySize += (int) (block.data_size + block.zero_size);
            }

            // source data
            int sourceSize = (int) (data.BaseStream.Length - header.exe_offset);
            data.BaseStream.Seek(header.exe_offset, SeekOrigin.Begin);
            byte[] sourceBuffer = data.ReadBytes(sourceSize);
            int sourceBufferOffs = 0;

            // sanity check
            const uint maxImageSize = 128 << 20;
            if (memorySize >= maxImageSize) {
                throw new InvalidDataException($"Computed image size is to big (0x{memorySize.ToString("X8")}), the exe offset = 0x{header.exe_offset.ToString("X8")}");
            }

            // Allocate in-place the XEX memory.
            byte[] memory = new byte[memorySize];

            // Destination memory pointers
            byte[] destMemory = memory;
            int destMemoryOffs = 0;

            // The decryption state is global for all blocks
            using (Aes aes = new AesManaged()) {
                aes.Mode = CipherMode.CBC; // ???
                aes.BlockSize = 128; // AES block size always 128
                aes.KeySize = 128;
                aes.Key = session_key;
                aes.Padding = PaddingMode.None;
                aes.IV = IV;
                using (ICryptoTransform decrypt = aes.CreateDecryptor()) {

                    // Copy/Decrypt blocks
                    for (int n = 0; n < blockCount; n++) {
                        // get the size of actual data and the zeros
                        XEXFileBasicCompressionBlock block = file_format_info.basic_blocks[n];
                        uint data_size = block.data_size;
                        uint zero_size = block.zero_size;

                        // decompress/copy data
                        XEXEncryptionType encType = file_format_info.encryption_type;
                        switch (encType) {
                            // no encryption, copy data
                            case XEX_ENCRYPTION_NONE: {
                                    Array.Copy(sourceBuffer, 0, destMemory, 0, data_size);
                                    break;
                                }

                            // AES
                            case XEX_ENCRYPTION_NORMAL: {
                                    int ct = sourceBufferOffs; // sourceBuffer
                                    int pt = destMemoryOffs; // destMemory;
                                    for (int o = 0; o < data_size; o += 16, ct += 16, pt += 16) {
                                        // Decrypt 16 uint8_ts from input -> output.
                                        decrypt.TransformBlock(sourceBuffer, ct, 16, destMemory, pt);

                                        // AesManaged should take care of the following.
                                        /*
                                        // XOR with previous
                                        for (int i = 0; i < 16; i++) {
                                            destMemory[pt + i] ^= ivec[i];
                                            ivec[i] = sourceBuffer[ct + i];
                                        }
                                        */
                                    }

                                    break;
                                }
                        }

                        // go to next block
                        sourceBufferOffs += (int) data_size;
                        destMemoryOffs += (int) (data_size + zero_size);
                    }

                }
            }

            // check if all source data was consumed
            int consumed = sourceBufferOffs;
            if (consumed > data.BaseStream.Length) {
                // Fix typo: To -> too -ade
                throw new InvalidDataException($"XEX: Too much source data was consumed by block decompression ({consumed} > {data.BaseStream.Length})");
            } else if (consumed < data.BaseStream.Length) {
                Console.WriteLine("XEX: {0} bytes of data was not consumed in block decompression (out of {1})", data.BaseStream.Length - consumed, data.BaseStream.Length);
            }

            // check if all data was outputed
            int numOutputed = destMemoryOffs;
            if (numOutputed > memorySize) {
                // Fix typo: To -> too -ade
                throw new InvalidDataException($"XEX: Too much data was outputed in block decompression ({numOutputed} > {memorySize})");
            } else if (numOutputed < memorySize) {
                Console.WriteLine("XEX: {0} bytes of data was not outputed in block decompression (out of {1})", memorySize - numOutputed, memorySize);
            }

            // loaded
            m_memoryData = memory;
            m_memorySize = memorySize;
        }

        private void LoadImageData(BinaryReader data) {
            // decompress and decrypts
            XEXCompressionType compType = file_format_info.compression_type;
            switch (compType) {
                case XEX_COMPRESSION_NONE: {
                        Console.WriteLine("XEX: image::Binary is not compressed");
                        LoadImageDataUncompressed(data);
                        return;
                    }

                case XEX_COMPRESSION_BASIC: {
                        Console.WriteLine("XEX: image::Binary is using basic compression (zero blocks)");
                        LoadImageDataBasic(data);
                        return;
                    }

                case XEX_COMPRESSION_NORMAL: {
                        Console.WriteLine("XEX: image::Binary is using normal compression");
                        LoadImageDataNormal(data);
                        return;
                    }
            }

            // unsupported compression
            throw new NotSupportedException($"Image is using unsupported compression mode {compType} and cannot be loaded");
        }

        void DecryptBuffer(byte[] key, byte[] inputData, int inputSize, byte[] outputData, int outputSize) {
            // no compression, just copy
            if (file_format_info.encryption_type == XEX_ENCRYPTION_NONE) {
                if (inputSize != outputSize) {
                    throw new InvalidDataException("inputSize != outputSize");
                }

                Array.Copy(inputData, 0, outputData, 0, inputSize);
                return;
            }

            // The decryption state is global for all blocks
            using (Aes aes = new AesManaged()) {
                aes.Mode = CipherMode.CBC; // ???
                aes.BlockSize = 128; // AES block size always 128
                aes.KeySize = 128;
                aes.Key = key;
                aes.Padding = PaddingMode.None;
                aes.IV = IV;
                using (ICryptoTransform decrypt = aes.CreateDecryptor()) {
                    int ct = 0; // inputData
                    int pt = 0; // outputData;
                    for (int n = 0; n < inputSize; n += 16, ct += 16, pt += 16) {
                        // Decrypt 16 uint8_ts from input -> output.
                        decrypt.TransformBlock(inputData, ct, 16, outputData, pt);

                        // AesManaged should take care of the following.
                        /*
                        // XOR with previous
                        for (int i = 0; i < 16; i++) {
                            destMemory[pt + i] ^= ivec[i];
                            ivec[i] = sourceBuffer[ct + i];
                        }
                        */
                    }
                }
            }

        }


    }
}
