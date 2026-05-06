using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaDynamicLibraryTest.Services;

public interface IDynamicLibraryCompiler
{
    Task<CompileResult> CompileAsync(string sourceCode, CancellationToken cancellationToken = default);
}
