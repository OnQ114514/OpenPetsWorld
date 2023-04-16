/*using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using static OpenPetsWorld.OpenPetsWorld;

namespace OpenPetsWorld
{
    public class Expression
    {
        private object instance;
        private MethodInfo method;

        /// <summary>
        /// 表达试运算
        /// </summary>
        /// <param name="expression">表达试</param>
        public Expression(string expression)
        {
            if (!expression.Contains("return"))
            {
                expression = "return " + expression + ";";
            }

            string className = "Expression";
            string methodName = "Compute";
            CompilerParameters p = new()
            {
                GenerateInMemory = true
            };
            var source = "using System;" + 
                         $"sealed class {className}" + 
                         "{" + 
                         "{" + 
                         $"public int {methodName}(int x)" +
                         "{" +
                         "{" +
                         $"{expression}" + 
                         "}" + 
                         "}" + 
                         "}" + 
                         "}";
            CompilerResults cr = new CSharpCodeProvider().CompileAssemblyFromSource(p, source);
            if (cr.Errors.Count > 0)
            {
                string msg = "Expression(\"" + expression + "\"): \n";
                foreach (CompilerError err in cr.Errors)
                {
                    msg += err + "\n";
                }

                throw new Exception(msg);
            }

            instance = cr.CompiledAssembly.CreateInstance(className);
            method = instance.GetType().GetMethod(methodName);
        }

        /// <summary>
        /// 处理数据
        /// </summary>
        public int Compute(PetData target, PetData myPet)
        {
            return (int)method.Invoke(instance, new object[] { target });
        }
    }
}*/