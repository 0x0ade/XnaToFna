using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using XnaToFna.ProxyForms;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;

namespace XnaToFna {
    public partial class XnaToFnaUtil : IDisposable {

        public static System.Reflection.ConstructorInfo m_UnverifiableCodeAttribute_ctor = typeof(System.Security.UnverifiableCodeAttribute).GetConstructor(Type.EmptyTypes);

        public void Stub(ModuleDefinition mod) {
            Log($"[Stub] Stubbing {mod.Assembly.Name.Name}");
            Modder.Module = mod;

            ApplyCommonChanges(mod, "Stub");

            // MonoMod needs to relink some types (f.e. XnaToFnaHelper) via FindType, which requires a dependency map.
            Log("[Stub] Mapping dependencies for MonoMod");
            Modder.MapDependencies(mod);

            Log($"[Stub] Stubbing");
            foreach (TypeDefinition type in mod.Types)
                StubType(type);

            Log($"[Stub] Pre-processing");
            foreach (TypeDefinition type in mod.Types)
                PreProcessType(type);

            Log($"[Stub] Relinking (MonoMod PatchRefs pass)");
            Modder.PatchRefs();

            Log($"[Stub] Post-processing");
            foreach (TypeDefinition type in mod.Types)
                PostProcessType(type);

            Log($"[Stub] Rewriting and disposing module\n");
            Modder.Module.Write(Modder.WriterParameters);
            // Dispose the module so other modules can read it as a dependency again.
            Modder.Module.Dispose();
            Modder.Module = null;
            Modder.ClearCaches(moduleSpecific: true);
        }

        public void StubType(TypeDefinition type) {
            foreach (FieldDefinition field in type.Fields) {
                field.Attributes &= ~FieldAttributes.HasFieldRVA;
            }

            foreach (MethodDefinition method in type.Methods) {
                if (method.HasPInvokeInfo)
                    method.PInvokeInfo = null;
                method.IsManaged = true;
                method.IsIL = true;
                method.IsNative = false;
                method.PInvokeInfo = null;
                method.IsPreserveSig = false;
                method.IsInternalCall = false;
                method.IsPInvokeImpl = false;

                MethodBody body = method.Body = new MethodBody(method);
                body.InitLocals = true;
                ILProcessor il = body.GetILProcessor();

                for (int i = 0; i < method.Parameters.Count; i++) {
                    ParameterDefinition param = method.Parameters[i];
                    if (param.IsOut || param.IsReturnValue) {
                        il.Emit(OpCodes.Ldarg, i);
                        il.EmitDefault(param.ParameterType, true);
                    }
                }

                il.EmitDefault(method.ReturnType ?? method.Module.TypeSystem.Void);
                il.Emit(OpCodes.Ret);
            }

            foreach (TypeDefinition nested in type.NestedTypes)
                StubType(nested);
        }

    }
}
