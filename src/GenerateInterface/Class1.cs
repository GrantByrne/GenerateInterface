using System;

namespace GenerateInterface
{
    /// <summary>
    /// Marks a class for automatic interface generation.
    /// The source generator will create an interface with all public members of the annotated class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class GenerateInterfaceAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the generated interface.
        /// If not specified, the interface name will be "I" + class name.
        /// </summary>
        public string? InterfaceName { get; set; }

        /// <summary>
        /// Gets or sets the namespace for the generated interface.
        /// If not specified, the interface will be generated in the same namespace as the class.
        /// </summary>
        public string? Namespace { get; set; }
    }
}
