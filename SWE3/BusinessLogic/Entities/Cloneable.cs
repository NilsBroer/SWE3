namespace SWE3.BusinessLogic.Entities
{
    public abstract class Cloneable
    {
        /// <summary>
        /// Users should let their classes inherit from this, to keep Cache in synch with database
        /// Not using this will make cached objects reference to their origin, instead of a copy
        /// </summary>
        public T Clone<T>()
        {
            return (T) this.MemberwiseClone();
        }
    }
}