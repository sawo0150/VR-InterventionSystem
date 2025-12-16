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
        Error,

        /// <summary>
        /// Respawn message when robot is hit by deer (Event 1)
        /// </summary>
        DeerRespawn,

        /// <summary>
        /// Respawn message when robot is hit by rolling stone (Event 1)
        /// </summary>
        StoneRespawn,

        /// <summary>
        /// Respawn message when robot is hit by children (Event 3)
        /// </summary>
        ChildrenRespawn,

        /// <summary>
        /// Warning message when robot collides with a child (Event 3)
        /// </summary>
        ChildWarning,

        /// <summary>
        /// Alert message shown when an event is activated
        /// </summary>
        Alert,

        /// <summary>
        /// Delivery message shown when robot successfully completes delivery and returns
        /// </summary>
        Delivery
    }
}
