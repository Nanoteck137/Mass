using System;
using System.Collections.Generic;
using System.Text;

using LLVMSharp;

class LLVMGenerator : CodeGenerator
{
    private LLVMModuleRef module;

    public LLVMGenerator(Resolver resolver)
        : base(resolver)
    {
        module = LLVMModuleRef.CreateWithName("NO NAME");
    }

    public override void Generate()
    {
    }

    public void DebugPrint()
    {
        module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        string str = module.PrintToString();
        Console.WriteLine(str);
    }

    public static void Test()
    {
        Lexer lexer = new Lexer("LLVM Code Generator Test", "");
        Parser parser = new Parser(lexer);
        Resolver resolver = new Resolver();
        LLVMGenerator gen = new LLVMGenerator(resolver);

        string[] code = new string[]
        {
            "var a: s32 = 123;",
        };

        foreach (string c in code)
        {
            lexer.Reset(c);
            lexer.NextToken();

            Decl decl = parser.ParseDecl();
            resolver.AddSymbol(decl);
        }

        resolver.ResolveSymbols();
        resolver.FinalizeSymbols();

        gen.Generate();
        gen.DebugPrint();
    }
}
