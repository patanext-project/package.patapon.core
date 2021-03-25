namespace PataNext.Client.Graphics
{
	public enum EActivationType
	{
        /// <summary>
        ///     Everytime the component will get updated
        /// </summary>
        Everytime = 0,

        /// <summary>
        ///     When seen by a renderer, the component will get updated
        /// </summary>
        Renderer = 1,

        /// <summary>
        ///     When the bounds are in camera range, the component will get updated
        /// </summary>
        Bounds = 2
	}
}