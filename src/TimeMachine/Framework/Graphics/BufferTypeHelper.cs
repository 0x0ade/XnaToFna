using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod;
using Mono.Cecil.Cil;
using Mono.Cecil;
using System.Runtime.InteropServices;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class BufferTypeHelper {

        public static System.Reflection.MethodInfo m_BufferTypeHelper_CreateVertexBuffer = typeof(BufferTypeHelper).GetMethod("CreateVertexBuffer");

        public static BufferUsage ConvertBufferUsage(BufferUsage usage)
            =>
            ((int) usage == 0) ? BufferUsage.None :
            ((int) usage == 64) ? BufferUsage.None : // Pixel
            ((int) usage == 8) ? BufferUsage.WriteOnly :
            usage; // ???

        public static MethodParser GenerateMethodParser(MethodParser parser)
            => delegate (MonoModder mod, MethodBody body, Instruction instr, ref int instri) {
                if (instr.OpCode == OpCodes.Newobj && (instr.Operand as MethodReference)?.GetFindableID() ==
                    "System.Void Microsoft.Xna.Framework.Graphics.VertexBuffer::.ctor(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.Int32,Microsoft.Xna.Framework.Graphics.BufferUsage)"
                )
                    return FixTypelessVertexBufferConstructor(mod, body, instr, ref instri);

                return parser?.Invoke(mod, body, instr, ref instri) ?? true;
            };

        // Format:
        // newobj instance void [FNA]Microsoft.Xna.Framework.Graphics.VertexBuffer::.ctor(class [FNA]Microsoft.Xna.Framework.Graphics.GraphicsDevice, int32, valuetype [FNA]Microsoft.Xna.Framework.Graphics.BufferUsage)
        // junk
        // callvirt instance void [FNA]Microsoft.Xna.Framework.Graphics.VertexBuffer::SetData<WE WANT THIS TYPE HERE>(!!0[])
        public static bool FixTypelessVertexBufferConstructor(MonoModder mod, MethodBody body, Instruction instr, ref int instri) {
            Instruction newobj = instr;
            Instruction callSetData = null;
            GenericInstanceMethod setData = null;

            ILProcessor il = body.GetILProcessor();

            for (int i = instri; i < body.Instructions.Count; i++) {
                Instruction call = body.Instructions[i];
                if (call.OpCode != OpCodes.Callvirt)
                    continue;
                GenericInstanceMethod method = call.Operand as GenericInstanceMethod;
                if (method == null)
                    continue;
                if (method.DeclaringType.FullName == "Microsoft.Xna.Framework.Graphics.VertexBuffer" &&
                    method.Name == "SetData" &&
                    method.IsGenericInstance) {
                    callSetData = call;
                    setData = method;
                    break;
                }
            }
            if (callSetData == null)
                return true;

            GenericInstanceMethod create = new GenericInstanceMethod(body.method.Module.ImportReference(m_BufferTypeHelper_CreateVertexBuffer));
            create.GenericArguments.Add(setData.GenericArguments[0]);

            il.InsertBefore(instr, new Instruction(OpCodes.Call, body.method.Module.ImportReference(create)));
            il.Remove(instr);
            instri--;
            return false;
        }

        public static VertexBuffer CreateVertexBuffer<T>(
            GraphicsDevice graphicsDevice,
            int sizeInBytes,
            BufferUsage usage
        ) {
            Type t = typeof(T);
            int size = Marshal.SizeOf(t);
            return new VertexBuffer(graphicsDevice, t, sizeInBytes / size, ConvertBufferUsage(usage));
        }

    }
}
