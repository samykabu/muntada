using Moq;

namespace Muntada.SharedKernel.Tests;

/// <summary>
/// Base test fixture providing common setup and helper methods
/// for SharedKernel unit tests.
/// </summary>
public abstract class BaseTestFixture
{
    /// <summary>
    /// Creates a mock of the specified interface.
    /// </summary>
    /// <typeparam name="T">The interface type to mock.</typeparam>
    /// <returns>A new <see cref="Mock{T}"/> instance.</returns>
    protected static Mock<T> CreateMock<T>() where T : class
    {
        return new Mock<T>();
    }

    /// <summary>
    /// Creates a default cancellation token for test operations.
    /// </summary>
    protected static CancellationToken TestCancellationToken => CancellationToken.None;
}
