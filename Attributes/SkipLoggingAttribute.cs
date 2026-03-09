using System;

namespace JwtAuthApp.Attributes
{
    /// <summary>
    /// Атрибут для исключения контроллера или действия из логирования
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SkipLoggingAttribute : Attribute
    {
        /// <summary>
        /// Причина исключения из логирования (для документации)
        /// </summary>
        public string Reason { get; set; }

        public SkipLoggingAttribute(string reason = "")
        {
            Reason = reason;
        }
    }
}