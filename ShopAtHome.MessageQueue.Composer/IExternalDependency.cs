namespace ShopAtHome.MessageQueue.Composer
{
    /// <summary>
    /// Register implementations of this interface with your IoC container to allow the Composer application to query them when necessary
    /// </summary>
    /// <remarks>An example of this would be a database server</remarks>
    public interface IExternalDependency
    {
        /// <summary>
        /// Gets the current state of the dependency
        /// </summary>
        /// <returns></returns>
        DependencyInfo GetCurrentState();
    }
}
