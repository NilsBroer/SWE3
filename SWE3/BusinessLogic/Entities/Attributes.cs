using System;

namespace SWE3.BusinessLogic.Entities
{
    /// <summary>
    /// Allows the user to set restrictive properties for their database entries.
    /// We do not use these properties and they are only enforced by the sql-server itself.
    /// </summary>
    public class NotNullableAttribute : Attribute { }
    public class PrimaryKeyAttribute : Attribute { }
    public class UniqueAttribute : Attribute { }
}