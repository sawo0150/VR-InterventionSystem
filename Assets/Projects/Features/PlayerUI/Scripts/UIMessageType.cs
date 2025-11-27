namespace Project
{
    /// <summary>
    /// Types of UI messages that can be displayed to the player.
    /// Each type can have its own custom panel design.
    /// </summary>
    public enum UIMessageType
    {
        /// <summary>
        /// Warning messages (e.g., robot leaving safe zone)
        /// </summary>
        Warning,

        /// <summary>
        /// Status/informational messages (e.g., speed, battery level)
        /// </summary>
        Status,

        /// <summary>
        /// Hint messages to guide the player
        /// </summary>
        Hint,

        /// <summary>
        /// Error messages for critical issues
        /// </summary>
        Error
    }
}
