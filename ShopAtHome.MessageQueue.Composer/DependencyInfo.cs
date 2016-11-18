namespace ShopAtHome.MessageQueue.Composer
{
    public class DependencyInfo
    {
        public DependencyInfo(bool canScaleUp, int percentAvailableResources)
        {
            CanScaleUp = canScaleUp;
            PercentAvailableResources = percentAvailableResources;
        }

        /// <summary>
        /// A bool indicating if the dependency has available resources that the composer application can use
        /// </summary>
        public bool CanScaleUp { get; }

        /// <summary>
        /// What percentage of the dependency's resources are available to the composer application
        /// </summary>
        public int PercentAvailableResources { get; }
    }
}
