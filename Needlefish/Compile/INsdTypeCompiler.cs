using Needlefish.Schema;
using System.Text;

namespace Needlefish.Compile;

internal interface INsdTypeCompiler
{
    bool CanCompile(TypeDefinition typeDefinition);
    StringBuilder Compile(TypeDefinition typeDefinition);
}