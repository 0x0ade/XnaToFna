//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using System;
using System.IO;

using Mono.Cecil.Cil;
using Mono.Cecil;
using Mono.Cecil.Mdb;
using Mono.CompilerServices.SymbolWriter;

namespace XnaToFna.Mdb {

	public sealed class MdbReaderProvider : ISymbolReaderProvider {

        public ISymbolReader GetSymbolReader(ModuleDefinition module, string fileName) {
            Mixin.CheckModule(module);
            Mixin.CheckFileName(fileName);

            return new MdbReader(module, MonoSymbolFile.ReadSymbolFile(Mixin.GetMdbFileName(fileName), module.Mvid));
        }

        public ISymbolReader GetSymbolReader(ModuleDefinition module, Stream symbolStream) {
            Mixin.CheckModule(module);
            Mixin.CheckStream(symbolStream);

            var file = MonoSymbolFile.ReadSymbolFile(symbolStream);
            if (module.Mvid != file.Guid) {
                var file_stream = symbolStream as FileStream;
                if (file_stream != null)
                    throw new MonoSymbolFileException("Symbol file `{0}' does not match assembly", file_stream.Name);

                throw new MonoSymbolFileException("Symbol file from stream does not match assembly");
            }
            return new MdbReader(module, file);
        }
    }

#if !READ_ONLY

	public sealed class MdbWriterProvider : ISymbolWriterProvider {

        public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName) {
            Mixin.CheckModule(module);
            Mixin.CheckFileName(fileName);

            return new MdbWriter(module.Mvid, fileName);
        }

        public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream) {
            throw new NotImplementedException();
        }
    }

#endif
}
