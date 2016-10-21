using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Helpers
{
    internal static class TypeHelper
    {
        /// <summary>
        /// Generic metodlarda ismin doğru basılmasını sağlar.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetOriginalName(this Type type)
        {
            var typeName = type.FullName.Replace(type.Namespace + ".", "");

            var provider = System.CodeDom.Compiler.CodeDomProvider.CreateProvider("CSharp");
            var reference = new System.CodeDom.CodeTypeReference(typeName);

            return provider.GetTypeOutput(reference);
        }
    }
}
