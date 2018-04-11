using System.Threading;
using System.Threading.Tasks;

namespace LogReader.Interfaces
{
    /// <summary>
    /// A generic file reader that allows infinite reading (until cancelled).
    /// </summary>
    public interface IFileReader
    {
        Task StartReadAsync(CancellationToken ct);
    }
}
