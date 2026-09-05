using System;

namespace Inventory.Application.Exceptions;

public class EntityInUseException : Exception
{
    public EntityInUseException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
