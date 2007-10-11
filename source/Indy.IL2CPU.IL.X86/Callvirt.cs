using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CPU = Indy.IL2CPU.Assembler;
using CPUx86 = Indy.IL2CPU.Assembler.X86;

namespace Indy.IL2CPU.IL.X86 {
	[OpCode(Code.Callvirt, true)]
	public class Callvirt: Op {
		private int mMethodIdentifier;
		private bool mHasReturn;
		private string mNormalAddress;
		private string mMethodDescription;
		public Callvirt(Instruction aInstruction, MethodInformation aMethodInfo)
			: base(aInstruction, aMethodInfo) {
			int xThisOffSet = (from item in aMethodInfo.Locals
							   select item.Offset + item.Size).LastOrDefault();
			MethodReference xMethod = aInstruction.Operand as MethodReference;
			if (xMethod == null) {
				throw new Exception("Unable to determine Method!");
			}
			MethodDefinition xMethodDef = Engine.GetDefinitionFromMethodReference(xMethod);
			mMethodDescription = new CPU.Label(xMethodDef).Name;
			if (xMethodDef.IsStatic || !xMethodDef.IsVirtual) {
				mNormalAddress = new CPU.Label(xMethod).Name;
				mHasReturn = !xMethod.ReturnType.ReturnType.FullName.StartsWith("System.Void");
				return;
			}
			mMethodIdentifier = Engine.GetMethodIdentifier(xMethodDef);
			Engine.QueueMethodRef(VTablesImplRefs.GetMethodAddressForTypeRef);
			MethodInformation xTheMethodInfo = Engine.GetMethodInfo(xMethodDef, xMethodDef, mMethodDescription, null);
			mHasReturn = xTheMethodInfo.ReturnSize != 0;
		}

		public override void DoAssemble() {
			if (!String.IsNullOrEmpty(mNormalAddress)) {
				Call(mNormalAddress);
			} else {
				if (Assembler.InMetalMode) {
					throw new Exception("Virtual methods not supported in Metal mode! (Called method = '" + mMethodDescription + "')");
				}
				Assembler.Add(new CPUx86.Pop("eax"));
				Assembler.Add(new CPUx86.Pushd("eax"));
				Assembler.Add(new CPUx86.Pushd("[eax]"));
				Assembler.Add(new CPUx86.Pushd("0" + mMethodIdentifier.ToString("X") + "h"));
				Call(new CPU.Label(VTablesImplRefs.GetMethodAddressForTypeRef).Name);
				Call("eax");
			}
			if (mHasReturn) {
				Pushd(4, "eax");
			}
		}
	}
}